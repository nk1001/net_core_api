using Core.Helper.IOC;

namespace Core.Helper.IOC
{
    public interface IUser : IHasId
    {

        string UserName { get; set; }
        string? LoginIp { get; set; }
        string? JwtToken { get; set; }
        string? JwtRefreshToken { get; set; }
        string NickName { get; set; }
        string? LastName { get; set; }
        string FirstName { get; set; }
        string PassWord { get; set; }
        string? PhotoUrl { get; set; }
        DateTime? LastLoginDate { get; set; }
        DateTime? Birthday { get; set; }
        int? Sex { get; set; }
        string? Address { get; set; }
        string? Phone { get; set; }
        string? Email { get; set; }
        string? ResetPasswordToken { get; set; }
        DateTime? LastResetPasswordDate { get; set; }          
    }
}
