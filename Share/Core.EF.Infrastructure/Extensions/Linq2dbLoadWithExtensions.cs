using System.Linq.Expressions;
using LinqToDB;

namespace Core.EF.Infrastructure.Extensions
{
    public static class Linq2dbLoadWithExtensions
    {

        public static List<string> GetDataMmyy(DateTime startDate, DateTime endDate, bool includeMargin, List<string> dbSchemas)
        {
            // Parse the date range with optional margin
            DateTime startDateTime = startDate.AddDays(includeMargin ? -3 : 0);
            DateTime endDateTime = endDate.AddDays(includeMargin ? 3 : 0);

            int startYear = startDateTime.Year;
            int startMonth = startDateTime.Month;
            int endYear = endDateTime.Year;
            int endMonth = endDateTime.Month;

            const string baseSchema = "HSOFTTAMANH";
            List<string> schemas = new();

            // Loop through the years and months within the range
            for (int year = startYear; year <= endYear; year++)
            {
                int monthStart = (year == startYear) ? startMonth : 1;
                int monthEnd = (year == endYear) ? endMonth : 12;

                for (int month = monthStart; month <= monthEnd; month++)
                {
                    string mmyy = $"{month:00}{year % 100:D2}";
                    string schemaName = $"{baseSchema}{mmyy}";
                    if (IsSchemaExists(schemaName, dbSchemas))
                    {
                        schemas.Add(schemaName);
                    }

                }
            }

            return schemas;
        }
        private static bool IsSchemaExists(string schemaName, List<string> dbSchemas)
        {
            return dbSchemas != null && dbSchemas.Any(schema => schema.Equals(schemaName.ToUpper(), StringComparison.OrdinalIgnoreCase));
        }

        public static IQueryable<T> LoadWithDynamic<T>(this IQueryable<T> query, List<string> navigationPaths)
            where T : class
        {
            foreach (var path in navigationPaths)
            {
                var properties = path.Split('.');
                query = ApplyLoadWithPath(query, properties);
            }

            return query;
        }

        private static IQueryable<T> ApplyLoadWithPath<T>(IQueryable<T> query, string[] properties) where T : class
        {
            Type currentType = typeof(T);
            Expression currentExpression = null;
            ParameterExpression parameter = Expression.Parameter(currentType, "e");

            for (var i = 0; i < properties.Length;)
            {
                var propertyInfo = currentType.GetProperty(properties[i]);
                if (propertyInfo == null)
                    throw new InvalidOperationException(
                        $"Property '{properties[i]}' not found on type '{currentType.Name}'.");

                var propertyExpression = Expression.Property(currentExpression ?? parameter, propertyInfo);
                {
                    var loadWithQueryable =
                        query.LoadWith(
                            Expression.Lambda<Func<T, object>>(Expression.Convert(propertyExpression, typeof(object)),
                                parameter)!);
                    currentExpression = propertyExpression;
                    for (int ix = 1; ix < properties.Length; ix++)
                    {
                        currentType = propertyInfo.PropertyType.IsGenericType
                            ? propertyInfo.PropertyType.GetGenericArguments()[0]
                            : propertyInfo.PropertyType;

                        propertyInfo = currentType.GetProperty(properties[ix]);
                        if (propertyInfo == null)
                            throw new InvalidOperationException(
                                $"Property '{properties[ix]}' not found on type '{currentType.Name}'.");
                        parameter = Expression.Parameter(currentType, "e");
                        propertyExpression = Expression.Property(parameter, propertyInfo);

                        var previousExpression = currentExpression;
                        currentExpression = Expression.Property(parameter, propertyInfo);

                        var lambda = Expression.Lambda<Func<object, object>>(
                            Expression.Convert(currentExpression, typeof(object)),
                            Expression.Parameter(typeof(object), ((MemberExpression)previousExpression).Member.Name)
                        );
                        loadWithQueryable = loadWithQueryable.ThenLoad(lambda!);
                        currentExpression = propertyExpression;
                    }

                    query = loadWithQueryable;
                }
                break;


            }

            return query;
        }




        public static IQueryable<T> LoadWithDynamic<T>(this IQueryable<T> query) where T : class
        {
            var paths = LinqIncludeExtensions.GetForeignKeyPaths(typeof(T));
            return LoadWithDynamic(query, paths);
        }

       
    }

}
