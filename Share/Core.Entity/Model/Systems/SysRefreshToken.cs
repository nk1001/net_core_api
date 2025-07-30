using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.IOC;
using Core.Helper.Model;

namespace Core.Entity.Model.Systems
{
    
    [Table(name: "SysRefreshToken")]
    public class SysRefreshToken : DependencyEntityEfService<SysRefreshToken>, IHasId,ICompanyBaseEntity
    {
        public string ID { get; set; }
        [Required]
        public string Token { get; set; } = string.Empty;

        public string UserID { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }
        public int? CompanyID { get; set; }
    }
}
