using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    
    [Table(name: "SysRefreshToken")]
    public class SysRefreshToken : DependencyEntityEfService<SysRefreshToken>, IHasId
    {
        public string ID { get; set; }
        [Required]
        public string Token { get; set; } = string.Empty;

        public string UserID { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }
    }
}
