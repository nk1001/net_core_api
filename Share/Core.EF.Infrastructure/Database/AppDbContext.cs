using Core.Helper.EFCore;
using Core.Helper.Extend;
using Core.Helper.IOC;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;

namespace Core.EF.Infrastructure.Database
{
    public class AppDbContext:DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
         
            
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            var entityTypes = AppDomain.CurrentDomain
             .GetAssemblies()
             .SelectMany(a =>
             {
                 try
                 {
                     return a.GetTypes();
                 }
                 catch (ReflectionTypeLoadException ex)
                 {
                     return ex.Types.Where(t => t != null)!;
                 }
             })
        .Where(t => typeof(IDependencyEntityEfService).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

            foreach (var type in entityTypes)
            {
                // Create an instance of the type
                if (Activator.CreateInstance(type) is not IDependencyEntityEfService instance)
                    continue;

                // Check if the entity should be included in migrations
                var isEntityMigration = instance.IsEntityMigration();

                // Get the EntityTypeBuilder and the EF Core entity type metadata
                var typeBuilder = modelBuilder.Entity(type);
                var entityType = modelBuilder.Model.FindEntityType(type);

                // Determine table name and schema
                var tableName = entityType?.GetTableName() ?? type.Name;
                var schema = entityType?.GetSchema() ?? "";
                if (schema.IsNullOrEmpty())
                {
                    // Configure the table mapping
                    typeBuilder.ToTable(tableName, table =>
                    {
                        if (!isEntityMigration)
                        {
                            table.ExcludeFromMigrations();
                        }
                    });
                }
                else
                {
                    // Configure the table mapping
                    typeBuilder.ToTable(tableName, schema, table =>
                    {
                        if (!isEntityMigration)
                        {
                            table.ExcludeFromMigrations();
                        }
                    });
                }
                
                instance.OnEntityConfiguring(modelBuilder);
            }


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entityType.ClrType;
                var entityBuilder = modelBuilder.Entity(clrType);

                foreach (var propInfo in clrType.GetProperties())
                {
                    if (propInfo.GetCustomAttribute<EfJsonConvertAttribute>() == null)
                        continue;

                    // Check if the property is a List<T> (for any T)
                    if (propInfo.PropertyType.IsGenericType &&
                        propInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var elementType = propInfo.PropertyType.GetGenericArguments()[0];
                        var propertyBuilder = entityBuilder.Property(propInfo.Name);

                        // Use ValueConverter dynamically
                        var converterType = typeof(JsonListConverter<>).MakeGenericType(elementType);
                        var converter = Activator.CreateInstance(converterType);

                        var converterMethod = typeof(PropertyBuilder)
                            .GetMethod(nameof(PropertyBuilder.HasConversion), new[] { typeof(ValueConverter) });

                        converterMethod?.Invoke(propertyBuilder, new[] { converter });
                    }
                }
            }
            base.OnModelCreating(modelBuilder);
        }
    }


    
}
