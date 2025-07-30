using System.Linq.Expressions;

namespace Core.Helper.IService
{
    public interface IServiceBase<T> : IAsyncService<T>, IDisposable where T : class
    {
    }
    public interface IAsyncService<T>
    {

        Task<T?> FindAsync(params object[] primaryKeys);
        Task<List<T>> GetAsync(Expression<Func<T, bool>> expression, string? includes = null);
        Task<(List<T>, long)> GetAsync(Expression<Func<T, bool>> expression, List<OrderByInfo> orders,
            int pageSize = 20, int pageIndex = 1);
        Task<bool> AddAsync(T item);
        Task<bool> DeleteAsync(T item);
        Task<bool> UpdateAsync(T item);
      

    }
    public class OrderByInfo
    {
        public string PropertyName { get; set; }
        public SortDirection Direction { get; set; }
        public bool Initial { get; set; }
    }

    public enum SortDirection
    {
        Ascending = 0,
        Descending = 1
    }
}
