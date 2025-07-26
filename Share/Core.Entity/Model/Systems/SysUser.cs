using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Helper.Attribute;
using Core.Helper.EFCore;
using Core.Helper.IOC;

namespace Core.Entity.Model.Systems
{
    [WebAuthorize("SysUser")]
    [Table("SysUser")]
    public class SysUser :  DependencyEntityEfService<SysUser>, IUser
    {
        [Key]
        public string ID { get; set; }
        public string UserName { get; set; }
        public string? LoginIp { get; set; }
        public string? JwtToken { get; set; }
        public string? JwtRefreshToken { get; set; }
        public string? NickName { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? PassWord { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? Birthday { get; set; }
        public int? Sex { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? ResetPasswordToken { get; set; }
        public DateTime? LastResetPasswordDate { get; set; }
        [NotMapped]

        public Dictionary<string, string>? MappingObject { get; set; }
        public string? GetMapping(string key)
        {
            throw new NotImplementedException();
        }
        [EfJsonConvert]
        public List<string>? Roles { get; set; }
    }
}
