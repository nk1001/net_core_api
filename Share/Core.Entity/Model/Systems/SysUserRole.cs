using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysRole")]
    [Table("SysUserRole")]
    public class SysUserRole : DependencyEntityEfService<SysUserRole>, IHasId
    {
        [Key]
        public string ID { get; set; }
        public string? UserId { get; set; }
        public string? RoleId { get; set; }

    }
}
