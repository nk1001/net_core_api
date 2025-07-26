using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Core.Helper.Attribute;

namespace Core.EF.WebApi.Helper
{
    public class GenericControllerRouteConvention : IControllerModelConvention
    {
        public static ConcurrentDictionary<string, Type> MappingTypes = new ConcurrentDictionary<string, Type>();
        public void Apply(ControllerModel controller)
        {
            if (controller.ControllerType.IsGenericType)
            {
                var genericType = controller.ControllerType.GenericTypeArguments[0];
                var customNameAttribute = genericType.GetCustomAttribute<GeneratedApiControllerAttribute>();
                var webAuthorizeAttribute = genericType.GetCustomAttribute<WebAuthorizeAttribute>();

                if (customNameAttribute?.Route != null)
                {
                    var routeAttribute = new RouteAttribute(customNameAttribute.Route);
                    routeAttribute.Order = customNameAttribute.Order;
                    controller.Selectors.Add(new SelectorModel
                    {

                        AttributeRouteModel = new AttributeRouteModel(routeAttribute),
                    });
                    if (webAuthorizeAttribute == null)
                    {
                        MappingTypes.AddOrUpdate(customNameAttribute.Route, genericType, (key, oldValue) => genericType);
                    }
                    else
                    {
                        MappingTypes.AddOrUpdate(webAuthorizeAttribute._key, genericType, (key, oldValue) => genericType);
                    }

                }
                else
                {

                    controller.ControllerName = genericType.Name;
                    if (webAuthorizeAttribute == null)
                    {
                        MappingTypes.AddOrUpdate(genericType.Name, genericType, (key, oldValue) => genericType);
                    }
                    else
                    {
                        MappingTypes.AddOrUpdate(webAuthorizeAttribute._key, genericType, (key, oldValue) => genericType);
                    }

                }
            }
        }
    }
}
