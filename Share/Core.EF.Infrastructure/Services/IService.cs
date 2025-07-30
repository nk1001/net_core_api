using Core.EF.Infrastructure.Repository;
using Core.Helper.IService;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Core.EF.Infrastructure.Services
{
    public interface IServiceBase<T>: Core.Helper.IService.IServiceBase<T> where T : class
    {
        Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action);
        IRepository<T> Repository { get; set; }
        IQueryable<T> Queryable();
     
    }
    
}
