using Core.EF.Infrastructure.Services;
using Core.EF.WebApi;
using Core.Entity.Model.Systems;
using Core.Helper.IOC;
using Microsoft.AspNetCore.Mvc.Filters;




namespace Core.EF.WebApi.Authorize
{

    public class AuthorizeApiAction :IAuthorizeAction
    {
        public async Task<bool> AuthorizeCore(AuthorizationFilterContext context)
        {
            var applicationContext = ServiceLocator.GetService<IApplicationContext>();
            var keyAuthKey = context.RouteData.Values[DefaultAuthorizeAttribute.AuthorizeApiKeyScheme]?.ToString() ?? "*";
            var keyAuthActon = context.RouteData.Values[DefaultAuthorizeAttribute.AuthorizeApiActionScheme]?.ToString() ?? "*";
            if (keyAuthKey == "*")
            {
                return true;
            }
            var cUser = (SysUser?)await applicationContext?.CurrentUser()!;
            var rs = await ServiceLocator.GetService<IServiceBase<SysRole>>()!.GetAsync(item => item.Status == 1);
            var rolesUser = cUser?.Roles??new List<string>();

            var roles = rs.Where(t => rolesUser.Contains(t.ID));
            List<SysRoleMenuAction> list = new List<SysRoleMenuAction>();
            foreach (var role in roles)
            {   
                if (role.Actions == null || !role.Actions.Any())
                {
                    continue;
                }
                list.AddRange(role.Actions.Where(t => t.Status == 1 && (t.MenuKey??"").Split(':').Last() == keyAuthKey && t.IsActive == true));

            }
            if (list.Any())
            {
                if (keyAuthActon == "*")
                {
                    return true;
                }
                return list.Any(t => t.Status == 1 && t.ActionKey == keyAuthActon);
            }
            return false;
        }
    }
}
