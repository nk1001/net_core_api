using System.Linq.Expressions;
using LinqToDB;
using Microsoft.EntityFrameworkCore.Storage;
using Core.Helper.IService;
using Core.EF.Infrastructure.Repository;

namespace Core.EF.Infrastructure.Services
{
    public interface IServiceBase<T>: Core.Helper.IService.IServiceBase<T> where T : class
    {
        Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action);
        IRepository<T> Repository { get; set; }
        IQueryable<T> Queryable();
        ITable<T> Table(string schema);
        ITable<T> Table();
        Task<List<T>> GetAsyncLinqToDB(Expression<Func<T, bool>> expression, string? includes = null);
        Task<(List<T>, long)> GetAsyncLinqToDB(Expression<Func<T, bool>> expression, List<OrderByInfo> orders,
            int pageSize = 20, int pageIndex = 1, string? includes = null);
    }
    
}
