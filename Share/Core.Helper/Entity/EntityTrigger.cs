using Core.Helper.IOC;

namespace Core.Helper.Entity
{
    public abstract  class EntityTrigger<T>: IDependencyService where T:class
    {
        public virtual void Insert(T entity)
        {

        }
        public virtual void Update(T? oldEntity,T newEntity)
        {

        }
        public virtual void Delete(T entity)
        {

        }
        public virtual Task InsertAsync(T entity)
        {
            return Task.CompletedTask;
        }
        public virtual Task UpdateAsync(T? oldEntity, T newEntity)
        {
            return Task.CompletedTask;
        }
        public virtual Task DeleteAsync(T entity)
        {
            return Task.CompletedTask;
        }
    }

    public interface IInsert
    {

    }
    public interface IUpdate
    {

    }
    public interface IDelete
    {

    }
}
