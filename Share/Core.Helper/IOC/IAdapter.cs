using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Core.Helper.Model;

namespace Core.Helper.IOC
{
    public interface IApplicationContext
    {
        int? CompanyID { get; set; }
        Task<IUser?> CurrentUser();
    }
    public interface IDependencyService
    {

    }
    public interface IDependencyEntityEfService : IDependencyService
    {
        void OnValidation();
        void OnEntityConfiguring(ModelBuilder builder);
        bool IsEntityMigration();

    }
    public abstract class DependencyEntityEfService<T> :EditorEntity, IDependencyEntityEfService where T : EditorEntity
    {
        public virtual void OnValidation()
        {
            throw new NotImplementedException();
        }

        public virtual void OnEntityConfiguring(ModelBuilder builder)
        {
            builder.Entity<T>();
        }

        public bool IsEntityMigration()
        {
            return true;
        }
    }
    public interface IHasId
    {
        [Key]
        [StringLength(100)]
        string ID { get; }
    }

}