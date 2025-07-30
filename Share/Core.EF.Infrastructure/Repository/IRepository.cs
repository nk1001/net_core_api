using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;
using Core.Helper.IService;
using Core.EF.Infrastructure.Database;

namespace Core.EF.Infrastructure.Repository
{
    public interface IRepository<T>
    {

        AppDbContext DbContext { get; set; }
        Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action);
        Task<T?> Get(params object[] primaryKeys);
        IQueryable<T> Queryable();
        Task<List<T>> Get();
        Task<List<T>> Get(Expression<Func<T, bool>> expression, string? includes = null);
        Task<(Task<List<T>>, Task<long>)> Get(Expression<Func<T, bool>> expression, List<OrderByInfo> orders,
            int pageSize = 20, int pageIndex = 1, string? includes = null);
        Task<bool> Add(T item);
        Task<bool> Delete(T item);
        Task<bool> Delete(Expression<Func<T, bool>> expression);
        Task<bool> Update(T item);
        Task<long> Count(Expression<Func<T, bool>> expression);


    }
}
