using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Core.EF.Infrastructure.Extensions
{
    public static class LinqIncludeExtensions
    {
        public static List<string> GetForeignKeyPaths(Type type)
        {
            var paths = new List<string>();
            BuildPaths(null, type, "", paths, 0);
            return paths;
        }

        // Recursive method to build paths
        private static void BuildPaths(Type? parent, Type type, string currentPath, List<string> paths, int count)
        {
            foreach (var property in type.GetProperties())
            {
                var foreignKeyAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (foreignKeyAttr == null) continue;

                var propertyType = property.PropertyType;
                if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    propertyType = propertyType.GetGenericArguments()[0];
                }

                // Construct path
                var newPath = string.IsNullOrEmpty(currentPath) ? property.Name : $"{currentPath}.{property.Name}";
                paths.Add(newPath);

                if (parent == type)
                {
                    count++;
                    if (count <= 3)
                    {
                        BuildPaths(type, propertyType, newPath, paths, count);
                    }
                }
                else
                {
                    BuildPaths(type, propertyType, newPath, paths, 0);
                }

                // Recursively build paths for nested foreign key properties

            }
        }
        public static IQueryable<T> IncludeQuery<T>(this IQueryable<T> query,
            params Expression<Func<T, object>>[] includeProperties) where T : class
        {

            foreach (var property in includeProperties)
            {
                if (!(property.Body is MethodCallExpression))
                    query = query.Include(property);
                else
                {
                    var expression = property.Body as MethodCallExpression;

                    if (expression != null)
                    {
                        var include = GenerateInclude(expression);

                        query = query.Include(include);
                    }
                }
            }

            return query;
        }

        static string GenerateInclude(MethodCallExpression? expression)
        {
            var result = default(string);

            foreach (var argument in expression.Arguments)
            {
                if (argument is MethodCallExpression)
                    result += GenerateInclude(argument as MethodCallExpression) + ".";
                else if (argument is MemberExpression)
                    result += ((MemberExpression)argument).Member.Name + ".";
                else if (argument is LambdaExpression)
                    result += ((MemberExpression)(argument as LambdaExpression).Body).Member.Name + ".";
            }

            return result.TrimEnd('.');
        }
        public static IQueryable<T> IncludeMultiple<T>(this IQueryable<T> query,
            params string[]? includes) where T : class
        {
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => current.Include(include));
            }
            return query;
        }

       
    }
}
