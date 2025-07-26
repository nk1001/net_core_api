using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Core.Helper.Attribute;
using Core.EF.WebApi.Controllers;

namespace Core.EF.WebApi.Helper
{
    public class GenericTypeControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (Assembly referencedAssembly in AssemblyLoadContext.Default.Assemblies)
            {
                try
                {
                    var candidates = referencedAssembly.GetExportedTypes().Where(x => x.GetCustomAttributes<GeneratedApiControllerAttribute>().Any());

                    foreach (var candidate in candidates)
                    {
                        feature.Controllers.Add(typeof(BaseApiController<>).MakeGenericType(candidate).GetTypeInfo());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    
                }
               
            }

        }
    }
}
