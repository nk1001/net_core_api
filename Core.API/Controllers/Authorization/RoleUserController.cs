using Microsoft.AspNetCore.Mvc;
using Core.Entity.Model.Systems;
using Core.EF.Infrastructure.Services;
using Core.EF.WebApi.Controllers;

namespace Core.API.Controllers.Authorization
{
    [Route("api/roleuser", Order = 0)]
    /// <summary>
    /// Danh mục ROLE
    /// </summary>
    public class RoleUserController : BaseApiController<SysUserRole>
    {
        /// <summary>
        /// Init Controller
        /// </summary>
        /// <param name="service"></param>
        /// <param name="logger"></param>
        public RoleUserController(IServiceBase<SysUserRole> service, ILogger<SysUserRole> logger) : base(service, logger)
        {
            //service.Repository.DbContext.Database.Migrate();
        }
    }
}
