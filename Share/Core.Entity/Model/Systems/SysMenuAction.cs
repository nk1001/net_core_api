using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysMenu")]
    [Table("SysMenuAction")]
    public class SysMenuAction : DependencyEntityEfService<SysMenuAction>, IHasId
    {
        [Key]
        public string ID { get; set; }
        public virtual string? MenuId { get; set; }
        public string? MenuKey { get; set; }
        public string? ActionKey { get; set; }
        public string? ActionName { get; set; }
    }
}
