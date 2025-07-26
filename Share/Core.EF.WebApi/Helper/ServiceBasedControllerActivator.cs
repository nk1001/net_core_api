using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Core.EF.WebApi.Helper
{
    public class ServiceBasedControllerActivator : IControllerActivator
    {
        public object Create(ControllerContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }
            var controllerType = actionContext.ActionDescriptor.ControllerTypeInfo.AsType();
            var controller = actionContext.HttpContext.RequestServices.GetService(controllerType);
            if (controller==null)
            {
                var ctor= controllerType.GetConstructors().MaxBy(t=>t.GetParameters().Length);
                List<object?> list = new List<object?>();
                if (ctor != null)
                {
                    foreach (var itemParameter in ctor.GetParameters())
                    {
                       var obj=  actionContext.HttpContext.RequestServices.GetService(itemParameter.ParameterType);
                       list.Add(obj);
                    }

                }
                controller = Activator.CreateInstance(controllerType, args: list.ToArray());

            }
         
            return controller;

        }

        /// <inheritdoc />
        public virtual void Release(ControllerContext context, object controller)
        {
        }
    }
}
