using Core.EF.Infrastructure.Services;
using Core.Entity.Model.Systems;
using Core.Helper.IOC;

namespace Core.EF.WebApi
{
   
    public class ApplicationContext : IApplicationContext
    {
        public int? CompanyID { get; set; }

        public async Task<IUser?> CurrentUser()
        {
            if (!ServiceLocator.HttpContext?.User?.Identity?.IsAuthenticated??false)
            {
                return null;
            }
            var uName = ((System.Security.Claims.ClaimsIdentity)ServiceLocator.HttpContext!.User.Identity!).Name;
            return (await ServiceLocator.GetService<IServiceBase<SysUser>>()!.GetAsync(t => t.UserName == uName)).FirstOrDefault()!;
        }
    }
}
