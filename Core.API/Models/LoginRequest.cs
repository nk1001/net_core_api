using System.ComponentModel.DataAnnotations;

namespace Core.API.Models
{

    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; }
    }

    public class LogoutRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; }
    }
    public class SignatureRequest
    {
        [Required(ErrorMessage = "SignatureBody is required.")]
        public string SignatureBody { get; set; }
        [Required(ErrorMessage = "SignatureKey is required.")]
        public string SignatureKey { get; set; }
    }
}
