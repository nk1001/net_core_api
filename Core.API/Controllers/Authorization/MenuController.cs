using Core.Entity.Model.Systems;
using Microsoft.AspNetCore.Mvc;
using Core.EF.Infrastructure.Services;
using Core.EF.WebApi.Controllers;

namespace Core.API.Controllers.Authorization
{
    [Route("api/menu", Order = 0)]
    /// <summary>
    /// Danh mục MENU
    /// </summary>
    public class MenuController : BaseApiController<SysMenu>
    {

        /// <summary>
        /// Init Controller
        /// </summary>
        /// <param name="service"></param>
        /// <param name="logger"></param>
        public MenuController(IServiceBase<SysMenu> service, ILogger<SysMenu> logger) : base(service, logger)
        {
        }
    }
}
