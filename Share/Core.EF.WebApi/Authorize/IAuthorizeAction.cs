using Microsoft.AspNetCore.Mvc.Filters;

namespace Core.EF.WebApi.Authorize
{
    public interface IAuthorizeAction
    {
        public Task<bool> AuthorizeCore(AuthorizationFilterContext context);
    }
}
