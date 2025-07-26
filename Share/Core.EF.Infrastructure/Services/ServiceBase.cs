using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Core.Helper.Extend;
using Core.Helper.IService;
using Core.EF.Infrastructure.Database;
using Core.EF.Infrastructure.Extensions;
using Core.EF.Infrastructure.Repository;

namespace Core.EF.Infrastructure.Services
{
    public class ServiceBase<T> : Core.EF.Infrastructure.Services.IServiceBase<T> where T : class
    {
        public ServiceBase(AppDbContext context)
        {
            Repository = new RepositoryBase<T>(context);
        }
        public async Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action)
        {
            return await Repository.TransactionUsing(action);
        }
        public ITable<T> Table(string schema)
        {
            return Repository.DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema);
        }
        public ITable<T> Table()
        {
            return Repository.DbContext.Set<T>().ToLinqToDBTable();
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
            return await Repository.Queryable().Where(exp!).IncludeMultiple(LinqIncludeExtensions.GetForeignKeyPaths(typeof(T)).ToArray()).AsSplitQuery().FirstAsyncEF();

        }

        public async Task<List<T>> GetAsync()
        {
            return await Repository.Get();
        }

        public async Task<List<T>> GetAsync(Expression<Func<T, bool>> expression, string? includes = null)
        {
            return await Repository.Get(expression, includes);
        }

        public Task<(List<T>, long)> GetAsync(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, int pageSize = 20, int pageIndex = 1)
        {
            throw new NotImplementedException();
        }

        public async Task<(Task<List<T>>, Task<long>)> GetAsync(Expression<Func<T, bool>> expression,
            List<OrderByInfo> orders, int pageSize = 20, int pageIndex = 1, string? includes = null)
        {

            return await Repository.Get(expression, orders, pageSize, pageIndex,includes:includes);

        }

        public async Task<bool> AddAsync(T item)
        {
            return await Repository.Add(item);
        }

        public async Task<bool> DeleteAsync(T item)
        {
            return await Repository.Delete(item);
        }

        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> expression)
        {
            return await Repository.Delete(expression);
        }

        public async Task<bool> UpdateAsync(T item)
        {

            return await Repository.Update(item);
        }

        public async Task<List<T>> GetAsyncSchema(Expression<Func<T, bool>> expression, DateTime? filterFromDate, DateTime? filterToDate, string? includes = null)
        {

            return await Repository.GetSchema(expression, filterFromDate, filterToDate, includes);
        }

        public async Task<(List<T>, long)?> GetAsyncSchema(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, DateTime? filterFromDate, DateTime? filterToDate,
            int pageSize = 20, int pageIndex = 1, string? includes = null)
        {
            var rs= await Repository.GetSchema(expression, orders, filterFromDate, filterToDate, pageSize, pageIndex, includes: includes);
            await Task.WhenAll(rs.Item1, rs.Item2);
            return (rs.Item1.Result, rs.Item2.Result);
        }

       
        
        public async Task<long> CountAsync(Expression<Func<T, bool>> expression)
        {
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
           /* List<T> rs = new List<T>();
            if (includes == null)
            {
                var state = Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
                Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                rs = await Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).LoadWithDynamic().ToListAsyncLinqToDB();
                Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;
            }
            else
            {
                if (includes.IsNullOrWhiteSpace())
                {
                    var state = Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
                    Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    rs = await Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).ToListAsyncLinqToDB();
                    Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;

                }
                else
                {
                    var state = Repository.DbContext.ChangeTracker.QueryTrackingBehavior;
                    Repository.DbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    rs = await Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).LoadWithDynamic(includes.Split(',').ToList()).ToListAsyncLinqToDB();
                    Repository.DbContext.ChangeTracker.QueryTrackingBehavior = state;
                }
            }
            return rs;*/
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
                /*if (includes == null)
                {
                    var tList = Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).LoadWithDynamic().OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                        .ToListAsyncLinqToDB();

                    return Task.FromResult((tList, tCount));
                }
                else
                {
                    if (includes.IsNullOrWhiteSpace())
                    {
                        var tList = Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                            .ToListAsyncLinqToDB();
                        return Task.FromResult((tList, tCount));
                    }
                    else
                    {
                        var tList = Repository.DbContext.Set<T>().ToLinqToDBTable().AsNoTracking().Where(expression).LoadWithDynamic(includes.Split(',').ToList()).OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                           .ToListAsyncLinqToDB();
                        return Task.FromResult((tList, tCount));
                    }

                }*/
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

        async Task<(List<T>, long)> IAsyncService<T>.GetAsyncSchema(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, DateTime? filterFromDate, DateTime? filterToDate, int pageSize, int pageIndex, string? includes)
        {
            var rs = await Repository.GetSchema(expression, orders, filterFromDate, filterToDate, pageSize, pageIndex, includes: includes);
            await Task.WhenAll(rs.Item1, rs.Item2);
            return (rs.Item1.Result, rs.Item2.Result);
        }
    }
}
