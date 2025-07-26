using System.Linq.Expressions;
using EFCoreSecondLevelCacheInterceptor;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Core.Helper.Extend;
using Core.Helper.IService;
using Core.EF.Infrastructure.Database;
using Core.EF.Infrastructure.Extensions;

namespace Core.EF.Infrastructure.Repository
{
    internal class RepositoryBase<T> :IRepository<T>,IAsyncDisposable where T : class
    {
     
        public RepositoryBase(AppDbContext dbContext)
        {
            DbContext = dbContext;
        

        }

        public async Task<(bool, Exception?)> TransactionUsing(Func<IDbContextTransaction?, Task> action)
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            {
                try
                {
                    await action(transaction);
                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    return (false, e);
                }
            }

        }

        public AppDbContext DbContext { get; set; }
        public IDbContextTransaction? ContextTransaction { get; set; } = null;

        public async Task CreateTransaction()
        {
            ContextTransaction ??= await DbContext.Database.BeginTransactionAsync();
        }
        public async Task CommitTransaction()
        {
            if (ContextTransaction != null)
            {
                await ContextTransaction.CommitAsync();
                ContextTransaction = null;
            }

        }
        public async Task RollbackTransaction()
        {
            if (ContextTransaction != null)
            {
                await ContextTransaction.RollbackAsync();
                ContextTransaction = null;
            }
        }
        public async Task<T?> Get(params object[] primaryKeys)
        {
            return await DbContext.Set<T>().FindAsync(primaryKeys).AsTask();
        }

        public IQueryable<T> Queryable()
        {
            return DbContext.Set<T>().AsQueryable();
        }

        public async Task<List<T>> Get()
        {
            return await DbContext.Set<T>().AsNoTracking().ToListAsyncEF();
        }

        public async Task<List<T>> Get(Expression<Func<T, bool>> expression, string? includes = null)
        {
           
            if (includes == null)
            {
                return await DbContext.Set<T>().AsNoTracking().Where(expression).Cacheable(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(5)).AsSplitQuery().ToListAsyncEF();
            }
            else
            {
                if (includes.IsNullOrWhiteSpace())
                {
                    return await DbContext.Set<T>().AsNoTracking().Where(expression).Cacheable(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(5)).AsSplitQuery().ToListAsyncEF();
                }
                else
                {
                    return await DbContext.Set<T>().AsNoTracking().Where(expression).IncludeMultiple(includes.Split(',')).Cacheable(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(5)).AsSplitQuery().ToListAsyncEF();
                }
            }


        }

        public Task<(Task<List<T>>, Task<long>)> Get(Expression<Func<T, bool>> expression,
            List<OrderByInfo> orders,
            int pageSize = 20, int pageIndex = 1, string? includes = null)
        {
            var tCount = Count(expression);
            if (includes == null)
            {
                var tList = DbContext.Set<T>().AsNoTracking().Where(expression).OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                    .AsSplitQuery().ToListAsyncEF();
                return Task.FromResult((tList, tCount));
            }
            else
            {
                if (includes.IsNullOrWhiteSpace())
                {
                    var tList = DbContext.Set<T>().AsNoTracking().Where(expression).OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                        .AsSplitQuery().ToListAsyncEF();
                    return Task.FromResult((tList, tCount));
                }
                else
                {
                    var tList = DbContext.Set<T>().AsNoTracking().Where(expression).IncludeMultiple(includes.Split(',')).OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize)
                        .AsSplitQuery().ToListAsyncEF();
                    return Task.FromResult((tList, tCount));
                }

            }
        }
        public async Task<bool> Add(T item)
        {
            await DbContext.Set<T>().AddAsync(item);
            var rec = await DbContext.SaveChangesAsync();
            return rec > 0;
        }
        public async Task<bool> Delete(T item)
        {
            DbContext.Set<T>().Update(item);
            var rec = await DbContext.SaveChangesAsync();
            return rec > 0;
        }
        public async Task<bool> Delete(Expression<Func<T, bool>> expression)
        {
            var listItem = await Get(expression);
            DbContext.Set<T>().RemoveRange(listItem);
            var rec = await DbContext.SaveChangesAsync();
            return rec > 0;
        }

        public async Task<bool> Update(T item)
        {
            try
            {

                var ex = DbContext.BuildFindKey(item);
                if (ex == null)
                {
                    throw new Exception($"Entity {typeof(T)} not support");
                }

                var includes= LinqIncludeExtensions.GetForeignKeyPaths(typeof(T)).ToArray();
                var _oldItem = await DbContext.Set<T>().Where(ex).IncludeMultiple(includes)
                    .FirstAsyncEF();
               // var _oldItem = await DbContext.FindAsync<T>(GetKey(item));

                new ProxyObjectMergeOpt().MergeProp(ref _oldItem, item);
                DbContext.Set<T>().Update(_oldItem);
                var rec = await DbContext.SaveChangesAsync();
                return rec > 0;
            }
            catch (Exception e)
            {

                throw;
            }

        }
        public async Task<long> Count(Expression<Func<T, bool>> expression)
        {

            return  await DbContext.Set<T>().AsNoTracking().AsQueryable().LongCountAsyncEF(expression);

        }
        public async Task<long> Count(Expression<Func<T, bool>> expression, string[] listSchemas)
        {
            List<IQueryable<T>> list = new List<IQueryable<T>>();
            foreach (var schema in listSchemas)
            {
                list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression));
            }

            return await list.Aggregate((c, n) => c.UnionAll(n)).LongCountAsyncLinqToDB();

        }
        public async Task<List<T>> GetSchema(Expression<Func<T, bool>> expression, DateTime? filterFromDate, DateTime? filterToDate, string? includes = null)
        {
            var lstSchemas = await DbContext.GetDatabaseSchema("HSOFTTAMANH");
            var listSchemas = Linq2dbLoadWithExtensions.GetDataMmyy(filterFromDate!.Value, filterToDate!.Value, false, lstSchemas);
           
            if (includes == null)
            {
                List<IQueryable<T>> list = new List<IQueryable<T>>();
                foreach (var schema in listSchemas)
                {
                    list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression)); 
                }

                return (await list.Aggregate((c, n) => c.UnionAll(n)).ToListAsyncLinqToDB());
            }
            else
            {
                if (includes.IsNullOrWhiteSpace())
                {
                    List<IQueryable<T>> list = new List<IQueryable<T>>();
                    foreach (var schema in listSchemas)
                    {
                        list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression));
                    }

                    return (await list.Aggregate((c, n) => c.UnionAll(n)).ToListAsyncLinqToDB());
                }
                else
                {
                    List<IQueryable<T>> list = new List<IQueryable<T>>();
                    foreach (var schema in listSchemas)
                    {
                        list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression).IncludeMultiple(includes.Split(',')));
                    }

                    return (await list.Aggregate((c, n) => c.UnionAll(n)).ToListAsyncLinqToDB());
                }
            }

        }

        public async Task<(Task<List<T>>, Task<long>)> GetSchema(Expression<Func<T, bool>> expression, List<OrderByInfo> orders, DateTime? filterFromDate, DateTime? filterToDate, int pageSize = 20,
            int pageIndex = 1, string? includes = null)
        {
            var lstSchemas = await DbContext.GetDatabaseSchema("HSOFTTAMANH");
            var listSchemas = Linq2dbLoadWithExtensions.GetDataMmyy(filterFromDate!.Value, filterToDate!.Value, false, lstSchemas);
            var tCount = Count(expression, listSchemas.ToArray());
            if (includes == null)
            {
                List<IQueryable<T>> list = new List<IQueryable<T>>();
                foreach (var schema in listSchemas)
                {
                    list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression));
                }

                var rs= list.Aggregate((c, n) => c.UnionAll(n));
                
                return  await Task.FromResult((rs.OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsyncLinqToDB(), tCount));
             

            }
            else
            {
                if (includes.IsNullOrWhiteSpace())
                {
                    List<IQueryable<T>> list = new List<IQueryable<T>>();
                    foreach (var schema in listSchemas)
                    {
                        list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression));
                    }

                    var rs = list.Aggregate((c, n) => c.UnionAll(n));

                    return await Task.FromResult((rs.OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsyncLinqToDB(), tCount));
                }
                else
                {
                    List<IQueryable<T>> list = new List<IQueryable<T>>();
                    foreach (var schema in listSchemas)
                    {
                        list.Add(DbContext.Set<T>().ToLinqToDBTable().SchemaName(schema).AsNoTracking().Where(expression).IncludeMultiple(includes.Split(',')));
                    }

                    var rs = list.Aggregate((c, n) => c.UnionAll(n));

                    return await Task.FromResult((rs.OrderBy(orders).Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsyncLinqToDB(), tCount));
              
                }

            }
        }

        public async ValueTask DisposeAsync()
        {

            await DbContext.DisposeAsync();
        }

        public void Dispose()
        {


            DbContext.Dispose();

        }
    }
}
