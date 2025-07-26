using Microsoft.AspNetCore.Mvc;
using Core.Entity.Model.Systems;
using Core.EF.Infrastructure.Services;
using Core.EF.WebApi.Controllers;

namespace Core.API.Controllers.Authorization
{
    [Route("api/[controller]", Order = 0)]
    /// <summary>
    /// Phân quyền ROLE MENU
    /// </summary>
    public class MenuRoleController : BaseApiController<SysRoleMenuAction>
    {
        /// <summary>
        /// Init Controller
        /// </summary>
        /// <param name="service"></param>
        /// <param name="logger"></param>
        public MenuRoleController(IServiceBase<SysRoleMenuAction> service, ILogger<SysRoleMenuAction> logger) : base(service, logger)
        {
        }
    }
}
