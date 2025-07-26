using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysRole")]
    [Table("SysRole")]
    public class SysRole : DependencyEntityEfService<SysRole>, IHasId
    {
        [Key]
        public string ID { get; set; }
        public string? RoleKey { get; set; }
        public string? RoleName { get; set; }
        [ForeignKey("RoleId")]
        public virtual List<SysRoleMenuAction>? Actions { get; set; } = new();
        [NotMapped]
        public virtual List<SysUserRole>? Users { get; set; } = new();
      

    }
}
