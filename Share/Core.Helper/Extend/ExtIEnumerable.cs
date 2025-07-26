using System.Diagnostics;
using System.Text;

namespace Core.Helper.Extend
{
    [DebuggerStepThrough]
    public static class ExtIEnumerable
    {
        public static string JoinStrings<TItem>(
           this IEnumerable<TItem> sequence,
           string separator,
           Func<TItem, string> converter)
        {
            StringBuilder seed = new StringBuilder();
            sequence.Aggregate<TItem, StringBuilder>(seed, (Func<StringBuilder, TItem, StringBuilder>)((builder, item) =>
            {
                if (builder.Length > 0)
                    builder.Append(separator);
                builder.Append(converter(item));
                return builder;
            }));
            return seed.ToString();
        }
        public static string JoinStrings<TItem>(this IEnumerable<TItem> sequence, string separator)
        {
            return sequence.JoinStrings<TItem>(separator, (Func<TItem, string>)(item => item.ToString()));
        }
        public static IEnumerable<T> Each<T>(this IEnumerable<T> source, Action<T> fun)
        {
            foreach (T obj in source)
                fun(obj);
            return source;
        }
        public static async Task<IEnumerable<T>> EachAync<T>(this IEnumerable<T> source, Func<T,Task> fun)
        {
            foreach (T obj in source)
                await fun(obj);
            return source;
        }
        public static List<TResult> ToList<T, TResult>(
          this IEnumerable<T> source,
          Func<T, TResult> fun)
        {
            List<TResult> result = new List<TResult>();
            source.Each<T>((Action<T>)(m => result.Add(fun(m))));
            return result;
        }
        public static bool IsGenericList(this Type o)
        {
            var isGenericList = false;

            var oType = o;

            if (oType.IsGenericType && ((oType.GetGenericTypeDefinition() == typeof(IEnumerable<>)) ||
                                        (oType.GetGenericTypeDefinition() == typeof(ICollection<>)) ||
                                        (oType.GetGenericTypeDefinition() == typeof(List<>)) ||
                                        (oType.GetGenericTypeDefinition() == typeof(IList<>))))
                isGenericList = true;

            return isGenericList;
        }
    }
}
