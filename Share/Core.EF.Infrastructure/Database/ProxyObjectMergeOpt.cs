using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Core.EF.Infrastructure.Database
{
    public class ProxyObjectMergeOpt
    {
        // Track already merged objects
        private readonly Dictionary<(Type type, object id), object> _mergedObjects = new();

        // Caches for compiled property accessors
        private readonly Dictionary<(Type, string), Func<object, object?>> _getCache = new();
        private readonly Dictionary<(Type, string), Action<object, object?>> _setCache = new();

        // Cache properties per type
        private readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new();
        private readonly Dictionary<Type, Dictionary<string, PropertyInfo>> _propertyMapCache = new();

        public void MergeProp<T>(ref T obj1, T obj2) where T : class
        {
            if (obj1 == null || obj2 == null) return;

            var type = obj1.GetType();
            var id = GetValue(obj1, "ID");
            var key = (type, id);

            if (_mergedObjects.ContainsKey(key))
            {
                Debug.WriteLine($"[Skip] Already merged: {type.Name} ID={id}");
                return;
            }

            _mergedObjects[key] = obj1;
            Debug.WriteLine($"[Merge] {type.Name} ID={id}");

            var properties = GetCachedProperties(type);
            var collectionProps = new List<PropertyInfo>();

            foreach (var prop in properties)
            {
                if (IsLazyUnloaded(prop, obj2))
                    continue;

                if (!prop.PropertyType.IsGenericType || !IsGenericList(prop.PropertyType))
                {
                    MergeSimpleOrNested(ref obj1, obj2, prop);
                }
                else
                {
                    collectionProps.Add(prop);
                }
            }

            foreach (var prop in collectionProps)
            {
                var listType = prop.PropertyType.GetGenericArguments()[0];
                var collectionKey = (prop.PropertyType, prop.Name, id);

                if (_mergedObjects.ContainsKey((listType, prop.Name)))
                    continue;

                _mergedObjects[(listType, prop.Name)] = obj1;

                var obj1List = GetValue(obj1, prop.Name) as System.Collections.IEnumerable;
                var obj2List = GetValue(obj2, prop.Name) as System.Collections.IEnumerable;

                var method = GetType().GetMethod(nameof(MergeArrayProp), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(listType);

                method.Invoke(this, new object[] { obj1List, obj2List, "ID" });
            }
        }

        private void MergeSimpleOrNested<T>(ref T obj1, T obj2, PropertyInfo prop) where T : class
        {
            var value1 = GetValue(obj1, prop.Name);
            var value2 = GetValue(obj2, prop.Name);

            if (value2 == null)
                return;

            if (prop.PropertyType.IsEnum || prop.PropertyType == typeof(string) || prop.PropertyType.IsValueType)
            {
                SetValue(obj1, prop.Name, value2);
            }
            else if (value1 != null)
            {
                var key = (prop.PropertyType, GetValue(value1, "ID"));
                if (_mergedObjects.TryGetValue(key, out var existing))
                {
                    SetValue(obj1, prop.Name, existing);
                }
                else
                {
                    _mergedObjects[key] = value1;
                    Debug.WriteLine($"[Nested] {prop.PropertyType.Name} ID={GetValue(value1, "ID")}");
                    var method = GetType().GetMethod(nameof(MergeProp))!
                        .MakeGenericMethod(prop.PropertyType);
                    method.Invoke(this, new object[] { value1, value2 });
                }
            }
        }

        private void MergeArrayProp<T>(IEnumerable<T>? obj1Raw, IEnumerable<T>? obj2Raw, string keyName) where T : class
        {
            if (obj2Raw == null)
                return;

            var list1 = obj1Raw?.ToList() ?? new List<T>();
            var list2 = obj2Raw.ToList();

            var dict1 = list1.ToDictionary(x => GetValue(x, keyName)!, x => x);
            var dict2 = list2.ToDictionary(x => GetValue(x, keyName)!, x => x);

            var idsToRemove = dict1.Keys.Except(dict2.Keys).ToList();
            var idsToAdd = dict2.Keys.Except(dict1.Keys).ToList();
            var idsToMerge = dict1.Keys.Intersect(dict2.Keys);

            // Remove non-matching
            foreach (var id in idsToRemove)
                list1.Remove(dict1[id]);

            // Add new
            foreach (var id in idsToAdd)
                list1.Add(dict2[id]);

            // Merge common
            foreach (var id in idsToMerge)
            {
                var item1 = dict1[id];
                var item2 = dict2[id];
                var key = (typeof(T), id);

                if (!_mergedObjects.ContainsKey(key))
                {
                    MergeProp(ref item1, item2);
                }
            }

            // Set back to original reference if needed
            if (obj1Raw is List<T> listRef)
            {
                listRef.Clear();
                listRef.AddRange(list1);
            }
        }

        private object? GetValue(object obj, string propName)
        {
            var key = (obj.GetType(), propName);
            if (!_getCache.TryGetValue(key, out var getter))
            {
                var param = Expression.Parameter(typeof(object));
                var casted = Expression.Convert(param, key.Item1);
                var property = Expression.Property(casted, propName);
                var convert = Expression.Convert(property, typeof(object));
                getter = Expression.Lambda<Func<object, object?>>(convert, param).Compile();
                _getCache[key] = getter;
            }
            return getter(obj);
        }

        private void SetValue(object obj, string propName, object? value)
        {
            var key = (obj.GetType(), propName);
            if (!_setCache.TryGetValue(key, out var setter))
            {
                var objParam = Expression.Parameter(typeof(object));
                var valueParam = Expression.Parameter(typeof(object));
                var castedObj = Expression.Convert(objParam, key.Item1);
                var property = key.Item1.GetProperty(propName)!;
                var castedValue = Expression.Convert(valueParam, property.PropertyType);
                var assign = Expression.Assign(Expression.Property(castedObj, propName), castedValue);
                setter = Expression.Lambda<Action<object, object?>>(assign, objParam, valueParam).Compile();
                _setCache[key] = setter;
            }
            setter(obj, value);
        }

        private PropertyInfo[] GetCachedProperties(Type type)
        {
            if (!_propertyCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                _propertyCache[type] = props;
                _propertyMapCache[type] = props.ToDictionary(p => p.Name);
            }
            return props;
        }

        private bool IsGenericList(Type type) =>
            type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(List<>);

        private bool IsLazyUnloaded(PropertyInfo prop, object obj)
        {
            // Lazy loading proxies: uninitialized navigation property will throw
            try
            {
                _ = prop.GetValue(obj);
                return false;
            }
            catch
            {
                return true;
            }
        }
    }
}
