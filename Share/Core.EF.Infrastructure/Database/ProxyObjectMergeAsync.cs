using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Core.EF.Infrastructure.Database
{
    public class ProxyObjectMergeAsync
    {
        private readonly Dictionary<string, object> _mergedObjects = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();
        private static readonly ConcurrentDictionary<(Type, string), Action<object, object>> PropertySetterCache = new();
        private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>> PropertyGetterCache = new();

        public async Task MergePropAsync<T>(T? obj1, T? obj2) where T : class
        {
            if (obj2 == null || obj1 == null) return;

            string key = $"{obj1.GetType()}_{GetPropValue(obj1, "ID")}";
            if (_mergedObjects.ContainsKey(key))
            {
                Debug.WriteLine($"Already Mapped > {obj1.GetType()} ID={GetPropValue(obj1, "ID")}");
                return;
            }

            Debug.WriteLine($"Merging {obj1.GetType()} ID={GetPropValue(obj1, "ID")}");
            _mergedObjects[key] = obj1;

            var properties = GetProperties(obj2.GetType());

            var simpleProps = new List<PropertyInfo>();
            var complexProps = new List<PropertyInfo>();
            var collectionProps = new List<PropertyInfo>();

            foreach (var prop in properties)
            {
                if (prop.PropertyType.IsGenericList())
                {
                    collectionProps.Add(prop);
                }
                else if (prop.PropertyType.IsPrimitiveType() || prop.PropertyType.IsEnum)
                {
                    simpleProps.Add(prop);
                }
                else
                {
                    complexProps.Add(prop);
                }
            }

            foreach (var prop in simpleProps)
            {
                SetPropValue(obj1, prop.Name, GetPropValue(obj2, prop.Name));
            }

            var complexTasks = complexProps.Select(async prop =>
            {
                var obj1Value = GetPropValue(obj1, prop.Name);
                var obj2Value = GetPropValue(obj2, prop.Name);
                if (obj1Value == null || obj2Value == null) return;

                string typeKey = $"{prop.PropertyType}_{GetPropValue(obj1Value, "ID")}";
                if (_mergedObjects.TryGetValue(typeKey, out var existing))
                {
                    SetPropValue(obj1, prop.Name, existing);
                    Debug.WriteLine($"Indirect Mapping {prop.Name} > {prop.PropertyType} ID={GetPropValue(obj1Value, "ID")}");
                }
                else
                {
                    _mergedObjects[typeKey] = obj1Value;
                    Debug.WriteLine($"Nested Merge > {prop.PropertyType} ID={GetPropValue(obj1Value, "ID")}");
                    await MergeDynamicAsync(obj1Value, obj2Value);
                }
            });

            await Task.WhenAll(complexTasks);

            var collectionTasks = collectionProps.Select(async prop =>
            {
                string arrayKey = $"{prop.PropertyType}_{prop.Name}_{GetPropValue(obj1, "ID")}";
                if (_mergedObjects.ContainsKey(arrayKey)) return;

                _mergedObjects[arrayKey] = obj1;
                await MergeArrayDynamicAsync(
                    GetPropValue(obj1, prop.Name),
                    GetPropValue(obj2, prop.Name),
                    "ID",
                    prop.PropertyType.GetGenericArguments()[0]);
            });

            await Task.WhenAll(collectionTasks);
        }

        public async Task MergeArrayPropAsync<T>(List<T>? obj1, List<T>? obj2, string keyName) where T : class
        {
            if (obj1 == null)
                return;

            if (obj2 == null)
            {
                obj1.Clear();
                return;
            }

            var toRemove = obj1
                .Where(item1 => !obj2.Any(item2 => Equals(GetPropValue(item1, keyName), GetPropValue(item2, keyName))))
                .ToList();

            foreach (var item in toRemove)
                obj1.Remove(item);

            var matches = from item2 in obj2
                          join item1 in obj1 on GetPropValue(item2, keyName) equals GetPropValue(item1, keyName) into gj
                          from match in gj.DefaultIfEmpty()
                          select new { Item2 = item2, Item1 = match };

            var tasks = matches.Select(async match =>
            {
                if (match.Item1 == null)
                {
                    obj1.Add(match.Item2);
                }
                else
                {
                    var ref1 = obj1.First(t => Equals(GetPropValue(t, keyName), GetPropValue(match.Item1, keyName)));
                    var ref2 = obj2.First(t => Equals(GetPropValue(t, keyName), GetPropValue(match.Item2, keyName)));
                    string refKey = $"{ref1.GetType()}_{GetPropValue(ref1, "ID")}";

                    if (!_mergedObjects.ContainsKey(refKey))
                    {
                        Debug.WriteLine($"Merging Array Item > {ref1.GetType()} ID={GetPropValue(ref1, "ID")}");
                        await MergePropAsync(ref1, ref2);
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task MergeDynamicAsync(object? obj1, object? obj2)
        {
            if (obj1 == null || obj2 == null) return;
            var method = GetType().GetMethod(nameof(MergePropAsync))?.MakeGenericMethod(obj1.GetType());
            if (method != null)
            {
                var task = (Task)method.Invoke(this, new[] { obj1, obj2 })!;
                await task;
            }
        }

        private async Task MergeArrayDynamicAsync(object? obj1, object? obj2, string keyName, Type elementType)
        {
            if (obj1 == null || obj2 == null) return;
            var method = GetType().GetMethod(nameof(MergeArrayPropAsync))?.MakeGenericMethod(elementType);
            if (method != null)
            {
                var task = (Task)method.Invoke(this, new[] { obj1, obj2, keyName })!;
                await task;
            }
        }

        #region Caching Helpers

        private static PropertyInfo[] GetProperties(Type type)
        {
            return PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }

        private static void SetPropValue(object obj, string propertyName, object? value)
        {
            var action = PropertySetterCache.GetOrAdd((obj.GetType(), propertyName), key =>
            {
                var paramObj = Expression.Parameter(typeof(object), "obj");
                var paramValue = Expression.Parameter(typeof(object), "value");

                var castObj = Expression.Convert(paramObj, key.Item1);
                var property = Expression.Property(castObj, key.Item2);
                var castValue = Expression.Convert(paramValue, property.Type);

                var body = Expression.Assign(property, castValue);
                return Expression.Lambda<Action<object, object>>(body, paramObj, paramValue).Compile();
            });

            action(obj, value!);
        }

        private static object? GetPropValue(object obj, string propertyName)
        {
            var func = PropertyGetterCache.GetOrAdd((obj.GetType(), propertyName), key =>
            {
                var paramObj = Expression.Parameter(typeof(object), "obj");

                var castObj = Expression.Convert(paramObj, key.Item1);
                var property = Expression.Property(castObj, key.Item2);
                var castResult = Expression.Convert(property, typeof(object));

                return Expression.Lambda<Func<object, object>>(castResult, paramObj).Compile();
            });

            return func(obj);
        }

        #endregion
    }

    internal static class TypeExtensions
    {
        public static bool IsGenericList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsPrimitiveType(this Type type)
        {
            return type.Namespace == "System";
        }
    }
}
