using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Runtime.Loader;
using Core.Helper.IOC;
using Core.Helper.Model;

namespace Core.Helper.NetCore
{
    public static class MvcServiceCollectionExtensions
    {
        public static void AddApiService<TService, TImplementation>(this IServiceCollection collection)
            where TService : class
            where TImplementation : class, TService
        {

            if (typeof(TService).GenericTypeArguments[0] != typeof(EditorEntity))
            {
                collection.TryAdd(ServiceDescriptor.Transient(typeof(TService).GenericTypeArguments[0], typeof(TService).GenericTypeArguments[0]));
                var descriptor = collection.FirstOrDefault(s => s.ServiceType == typeof(TService));
                if (descriptor != null) collection.Remove(descriptor);
                collection.AddScoped<TService, TImplementation>();
            }
          

        }
       
        public static void AddApiServiceWithType(this IServiceCollection services,Type type,Type implementationType)
        {

            typeof(MvcServiceCollectionExtensions).GetMethod("AddApiService")!
                .MakeGenericMethod(type, implementationType).Invoke(null, new object?[] { services });
        }
        public static void AddApiServiceCore(this IServiceCollection services, Type type, Type implementationType)
        {
            foreach (Assembly referencedAssembly in AssemblyLoadContext.Default.Assemblies)
            {
                foreach (var assembly in referencedAssembly.GetReferencedAssemblies())
                {
                    var _types = Assembly.Load(assembly).GetExportedTypes().Where(x => typeof(IDependencyService).IsAssignableFrom(x));

                    foreach (var _type in _types)
                    {
                        if (!_type.IsInterface && !_type.IsAbstract && _type.ToString() != typeof(object).ToString())
                        {
                            services.AddApiServiceWithType(type.MakeGenericType(_type), implementationType.MakeGenericType(_type));
                        }

                    }
                }
                var types = referencedAssembly.GetExportedTypes().Where(x => typeof(IDependencyService).IsAssignableFrom(x));
                foreach (var _type in types)
                {
                    if (!_type.IsInterface && !_type.IsAbstract && _type.ToString() != typeof(object).ToString())
                    {
                        services.AddApiServiceWithType(type.MakeGenericType(_type), implementationType.MakeGenericType(_type));
                    }

                }
            }

        }
    }
}
