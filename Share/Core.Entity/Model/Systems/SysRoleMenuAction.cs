using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysRole")]
    [Table("SysRoleMenuAction")]
    public class SysRoleMenuAction : DependencyEntityEfService<SysRoleMenuAction>, IHasId
    {
        [Key]
        public string ID { get; set; }
        public string? MenuKey { get; set; }
        public string? ActionKey { get; set; }
        public string? ActionName { get; set; }
        public bool? IsActive { get; set; }
        public string? RoleId { get; set; }
        public string? MenuId { get; set; }
    }
}
