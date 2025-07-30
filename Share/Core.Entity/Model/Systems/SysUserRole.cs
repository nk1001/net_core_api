using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;
using Core.Helper.Model;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysRole")]
    [Table("SysUserRole")]
    public class SysUserRole : DependencyEntityEfService<SysUserRole>, IHasId, ICompanyBaseEntity
    {
        [Key]
        public string ID { get; set; }
        public string? UserId { get; set; }
        public string? RoleId { get; set; }
        public int? CompanyID { get; set; }
    }
}
