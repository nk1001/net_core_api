using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;
using Core.Helper.Model;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysMenu")]
    [Table("SysMenuAction")]
    public class SysMenuAction : DependencyEntityEfService<SysMenuAction>, IHasId, ICompanyBaseEntity
    {
        [Key]
        public string ID { get; set; }
        public string? MenuId { get; set; }
        public string? MenuKey { get; set; }
        public string? ActionKey { get; set; }
        public string? ActionName { get; set; }
        public int? CompanyID { get; set; }
    }
}
