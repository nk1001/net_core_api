using Core.EF.Infrastructure.Database;
using Core.EF.Infrastructure.Extensions;
using Core.EF.Infrastructure.Repository;
using Core.Helper.Extend;
using Core.Helper.IOC;
using Core.Helper.IService;
using Core.Helper.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Core.EF.Infrastructure.Services
{
    public class ServiceBase<T> : IServiceBase<T> where T : class
    {
        IApplicationContext _applicationContext;
        public ServiceBase(AppDbContext context,IApplicationContext applicationContext)
        {
            _applicationContext=applicationContext;
            Repository = new RepositoryBase<T>(context, _applicationContext);
        }
        public async Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action)
        {
            return await Repository.TransactionUsing(action);
        }

        
        public IRepository<T> Repository { get; set; }

        public IQueryable<T> Queryable()
        {

            return Repository.Queryable();
        }
        
        public async Task<T?> FindAsync(params object[] primaryKeys)
        {
            var exp = Repository.DbContext.BuildFindKey<T>(primaryKeys);
            if (exp==null)
            {
                return null;
            }
            return await Repository.Queryable().Where(exp!).IncludeMultiple(LinqIncludeExtensions.GetForeignKeyPaths(typeof(T)).ToArray()).AsSplitQuery().FirstAsync();

        }

        public async Task<List<T>> GetAsync()
        {       
            Expression<Func<T, bool>> expression= x => true;
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T)) && _applicationContext.CompanyID!=null)
            {
                var pe = expression.Parameters[0];
                var _expression = Expression.AndAlso(expression.Body,
                    Expression.Equal(Expression.Property(pe, "CompanyID"), Expression.Constant(_applicationContext.CompanyID)));
                expression = expression.Update(_expression, expression.Parameters);
            }
            return await Repository.Get(expression);
        }

        public async Task<List<T>> GetAsync(Expression<Func<T, bool>> expression, string? includes = null)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T)) && _applicationContext.CompanyID!=null)
            {
                var pe = expression.Parameters[0];
                var _expression = Expression.AndAlso(expression.Body,
                    Expression.Equal(Expression.Property(pe, "CompanyID"), Expression.Constant(_applicationContext.CompanyID)));
                expression = expression.Update(_expression, expression.Parameters);
            }
            return await Repository.Get(expression, includes);
        }

        public async Task<(List<T>, long)> GetAsync(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, int pageSize = 20, int pageIndex = 1)
        {
           
            return await GetAsync(expression, orders, pageSize, pageIndex);
        }

        public async Task<(Task<List<T>>, Task<long>)> GetAsync(Expression<Func<T, bool>> expression,
            List<OrderByInfo> orders, int pageSize = 20, int pageIndex = 1, string? includes = null)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T)) && _applicationContext.CompanyID!=null)
            {
                var pe = expression.Parameters[0];
                var _expression = Expression.AndAlso(expression.Body,
                    Expression.Equal(Expression.Property(pe, "CompanyID"), Expression.Constant(_applicationContext.CompanyID)));
                expression = expression.Update(_expression, expression.Parameters);
            }
            return await Repository.Get(expression, orders, pageSize, pageIndex,includes:includes);

        }

        public async Task<bool> AddAsync(T item)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T)) && _applicationContext.CompanyID != null)
            {
                ((ICompanyBaseEntity)item).CompanyID = _applicationContext.CompanyID;

            }
            return await Repository.Add(item);
        }

        public async Task<bool> DeleteAsync(T item)
        {
            return await Repository.Delete(item);
        }

        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> expression)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T)) && _applicationContext.CompanyID!=null)
            {
                var pe = expression.Parameters[0];
                var _expression = Expression.AndAlso(expression.Body,
                    Expression.Equal(Expression.Property(pe, "CompanyID"), Expression.Constant(_applicationContext.CompanyID)));
                expression = expression.Update(_expression, expression.Parameters);
            }
            return await Repository.Delete(expression);
        }

        public async Task<bool> UpdateAsync(T item)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T))&& _applicationContext.CompanyID!=null)
            {
                ((ICompanyBaseEntity)item).CompanyID = _applicationContext.CompanyID;

            }
            return await Repository.Update(item);
        }

       
       
        
        public async Task<long> CountAsync(Expression<Func<T, bool>> expression)
        {
            if (typeof(ICompanyBaseEntity).IsAssignableFrom(typeof(T))&& _applicationContext.CompanyID!=null)
            {
                var pe = expression.Parameters[0];
                var _expression = Expression.AndAlso(expression.Body,
                    Expression.Equal(Expression.Property(pe, "CompanyID"), Expression.Constant(_applicationContext.CompanyID)));
                expression = expression.Update(_expression, expression.Parameters);
            }
            return await Repository.Count(expression);
        }
       
        public async Task<List<T>> GetAsyncLinqToDB(Expression<Func<T, bool>> expression, string? includes = null)
        {
            
            var state = Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
            try
            {
                includes ??= LinqIncludeExtensions.GetForeignKeyPaths(typeof(T)).ToArray().Join(",");
                Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return await GetAsync(expression, includes);
            }
            catch (Exception e)
            {
                return new List<T>();
            }
            finally
            {
                Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;
            }          
        }

        public async Task<(List<T>, long)> GetAsyncLinqToDB(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, int pageSize = 20, int pageIndex = 1,
            string? includes = null)
        {
            var state = Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
            Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var tCount = CountAsync(expression);
            try
            {
                includes ??= LinqIncludeExtensions.GetForeignKeyPaths(typeof(T)).ToArray().Join(",");
                var rs= await GetAsync(expression, orders, pageSize, pageIndex,
                    includes);
               await  Task.WhenAll(rs.Item1, rs.Item2);
               return (rs.Item1.Result, rs.Item2.Result);
               
            }
            catch (Exception e)
            {

                return (new List<T>(), 0L);
            }
            finally
            {
                Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;
            }
           
            
        }


        public void Dispose()
        {
           GC.SuppressFinalize(this);
           GC.WaitForPendingFinalizers();
        }

     
    }
}
