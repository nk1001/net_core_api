using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.IOC;
using Core.Helper.Model;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysCounter")]
    [Table("SysCounter")]
    public class SysCounter : DependencyEntityEfService<SysCounter>, IHasId, ICompanyBaseEntity
    {
        [Key]
        public string ID { get; set; }
        public string? Format { get; set; }
        public string? NumberFormat { get; set; }
        public long? Count { get; set; }
        public int? CompanyID { get; set; }
    }
}
