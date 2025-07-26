using Core.Entity.Model.Systems;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysMenu")]
    [Table("SysMenu")]
    public class SysMenu: DependencyEntityEfService<SysMenu>, IHasId
    {
     
        [Key]
        public string ID { get; set; }
        [Required]
        public string? MenuKey { get; set; }
        [Required]
        public string? MenuName { get; set; }
        public string? MenuDescription { get; set; }
        public string? MenuUrl { get; set; }
        public string? MenuIcon { get; set; }
        public int? MenuIndex { get; set; }
        [ForeignKey("MenuId")]
        public virtual List<SysMenuAction>? Actions { get; set; }=new();
        public string? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public virtual List<SysMenu>? Childrens { get; set; }
    }
}
