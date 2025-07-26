using Microsoft.AspNetCore.Mvc;
using Core.Entity.Model.Systems;
using Core.EF.Infrastructure.Services;
using Core.EF.WebApi.Controllers;

namespace Core.API.Controllers.Authorization
{
    [Route("api/role", Order = 0)]
    /// <summary>
    /// Danh mục ROLE
    /// </summary>
    public class RoleController : BaseApiController<SysRole>
    {
        /// <summary>
        /// Init Controller
        /// </summary>
        /// <param name="service"></param>
        /// <param name="logger"></param>
        public RoleController(IServiceBase<SysRole> service, ILogger<SysRole> logger) : base(service, logger)
        {
            //service.Repository.DbContext.Database.Migrate();
        }
    }
}
