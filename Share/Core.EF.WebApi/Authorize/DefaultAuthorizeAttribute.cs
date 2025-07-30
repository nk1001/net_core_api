using Core.EF.WebApi;
using Core.Helper.Attribute;
using Core.Helper.Extend;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;

namespace Core.EF.WebApi.Authorize
{



    public class InternalRequestAttribute : TypeFilterAttribute
    {
        public InternalRequestAttribute()
            : base(typeof(InternalRequestAuthorizeAttribute))
        {
            Order = -1;
            Arguments = new object[] { };
        }
    }
    public class InternalRequestAuthorizeAttribute : IAuthorizationFilter
    {

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (context.HttpContext.Request.Headers.Any(t => t.Key == "I-Request"))
            {
                var token = context.HttpContext.Request.Headers["I-Request"].ToString();
                var ipass = ServiceLocator.GetService<IConfiguration>()?.GetSection("RequestPassword").Value ?? "IRequest159";
                var result = ipass==token;
                if (result)
                {
                    return;
                }
            }
            context.Result = new ForbidResult();
           
        }
    }


    public class AllowAnonymousAttribute : TypeFilterAttribute
    {
        public AllowAnonymousAttribute()
            : base(typeof(AllowAnonymousAuthorizeAttribute))
        {
            Order = 0;
            Arguments = new object[] { };
        }
    }
    public class AllowAnonymousAuthorizeAttribute : IAuthorizationFilter
    {
       
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            
          
        }
    }


    
    public class AuthorizeAttribute : TypeFilterAttribute
    {
        readonly string _key;
        readonly string _action;
        public AuthorizeAttribute(string key = "UnSet", string action = "UnSet") : base(typeof(DefaultAuthorizeAttribute))
        {
            _key = key;
            _action = action;
            Order = 2;
            Arguments = new object[] { _key , _action };
        }
        public AuthorizeAttribute()
            : base(typeof(DefaultAuthorizeAttribute))
        {
            _key = "*";
            _action = "*";
            Order = 2;
            Arguments = new object[] { _key, _action };
        }
        

    }
    public class DefaultAuthorizeAttribute : IAsyncAuthorizationFilter
    {

        public const string AuthorizeApiKeyScheme = "AuthorizeApiKey";
        public const string AuthorizeApiActionScheme = "AuthorizeApiAction";
        string _key;
        string _action;
        public DefaultAuthorizeAttribute(string key = "*", string action = "*") : base()
        {
            _key = key;
            _action = action;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {

            if (context.Filters.Any(t => t is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
            {
                return;
            }
            if (context.Filters.Any(t=>t is AllowAnonymousAuthorizeAttribute))
            {
                return;
            }        
            if (context.HttpContext == null)
                throw new ArgumentNullException(nameof(context.HttpContext));

            //var applicationContext = ServiceLocator.GetService<IApplicationContext>();


            if (!(context.HttpContext.User.Identity?.IsAuthenticated??false))
            {
                context.Result = new UnauthorizedResult();
                return;

            }

            if (context.Filters.Any(t => t is InternalRequestAuthorizeAttribute))
            {
                return;
            }
            var ctype =
                ((Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor)
                .ControllerTypeInfo.AsType();

            
            if (_key == "UnSet")
            {
              
                if (ctype.GenericTypeArguments.Any())
                {

                    var type = ctype.GenericTypeArguments[0];
                    var csAttr = type.GetCustomAttribute<WebAuthorizeAttribute>();
                    if (csAttr!=null && csAttr!._key != "*" && _key == "UnSet")
                    {
                        context.RouteData.Values.SafeAdd(AuthorizeApiKeyScheme, csAttr._key);
                    }

                }
                else
                {
                    var csAttr = ctype
                        .GetCustomAttribute<WebAuthorizeAttribute>();
                    if (csAttr==null)
                    {
                        if (ctype.BaseType!=null && ctype.BaseType.GenericTypeArguments.Any())
                        {
                            csAttr = ctype.BaseType.GenericTypeArguments[0].GetCustomAttribute<WebAuthorizeAttribute>();
                        }
                    }

                    if (csAttr != null && csAttr._key != "*" && _key == "UnSet")
                    {
                        context.RouteData.Values.SafeAdd(AuthorizeApiKeyScheme, csAttr._key);
                    }
                }
            }

          
            if (!context.RouteData.Values.ContainsKey(AuthorizeApiKeyScheme))
            {
                if (_key != "UnSet")
                    context.RouteData.Values.SafeAdd(AuthorizeApiKeyScheme, _key);
            }
            if (!context.RouteData.Values.ContainsKey(AuthorizeApiActionScheme))
            {
                if (_action != "UnSet")
                    context.RouteData.Values.SafeAdd(AuthorizeApiActionScheme, _action);
            }
            
            var authorizes = ServiceLocator.GetServices<IAuthorizeAction>();
            foreach (var _action in authorizes)
            {
                var rs= await _action.AuthorizeCore(context);
                if (!rs)
                {

                    context.Result = new UnauthorizedResult();
                    break;
                }
            }
        }

    }

}
