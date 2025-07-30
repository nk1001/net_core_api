using System.Security.Claims;
using Core.EF.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entity.Model.Systems;
using Core.API.Models;
using Core.API.Security;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations; // For PasswordHasher
using Microsoft.AspNetCore.Http; // Add this at the top if not present
using System.IO; // For file operations

namespace Core.API.Controllers.Authorization
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
     
        private readonly JwtService _jwtService;
        private readonly AppDbContext _dbContext;

        public AuthController(JwtService jwtService, AppDbContext dbContext)
        {
            _jwtService = jwtService;
            _dbContext = dbContext;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Kiểm tra thông tin đăng nhập
            var user = _jwtService.ValidateUserCredentials(request.Username, request.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid username or password" });
            }
            var (accessToken, accessTokenExpires, refreshToken, refreshTokenExpires) = await _jwtService.GenerateTokens(user);

      

            return Ok(new
            {
                accessToken,
                accessTokenExpires,
                refreshToken,
                refreshTokenExpires
            });
        }

     
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            // Kiểm tra ModelState hợp lệ
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Tìm người dùng theo Refresh Token trong cơ sở dữ liệu
            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.JwtRefreshToken == request.RefreshToken);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            // Tạo mới Access Token và Refresh Token
            var (accessToken, accessTokenExpires, newRefreshToken, refreshTokenExpires) = await _jwtService.GenerateTokens(user);

            // Cập nhật Refresh Token mới vào cơ sở dữ liệu
            user.JwtRefreshToken = newRefreshToken;
            user.LastUpdateDate = DateTime.UtcNow;

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            // Trả về Access Token mới và Refresh Token mới
            return Ok(new
            {
                accessToken,
                accessTokenExpires,
                refreshToken = newRefreshToken,
                refreshTokenExpires
            });
        }
       
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            // Kiểm tra ModelState hợp lệ
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Tìm người dùng theo Refresh Token
            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.JwtRefreshToken == request.RefreshToken);
            if (user == null)
            {
                return NotFound(new { message = "Refresh token not found" });
            }

            // Xóa Refresh Token của user
            user.JwtRefreshToken = null;
            user.LastUpdateDate = DateTime.UtcNow;

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Logged out successfully" });
        }
 
        

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.UserName == request.UsernameOrEmail || u.Email == request.UsernameOrEmail);
            if (user == null)
                return NotFound(new { message = "If the account exists, a reset token has been sent." }); // Do not reveal user existence

            // Generate a reset token (GUID)
            user.ResetPasswordToken = Guid.NewGuid().ToString("N");
            user.LastResetPasswordDate = DateTime.UtcNow;

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            // TODO: Send the token to user's email (implement email sending as needed)
            // Example: await _emailService.SendResetPasswordEmail(user.Email, user.ResetPasswordToken);

            return Ok(new { message = "If the account exists, a reset token has been sent." });
        }

        [HttpPost("reset-password-with-token")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.ResetPasswordToken == request.Token);
            if (user == null)
                return BadRequest(new { message = "Invalid or expired reset token." });

            // Optionally, check token expiration (e.g., valid for 30 minutes)
            if (user.LastResetPasswordDate != null && user.LastResetPasswordDate.Value.AddMinutes(30) < DateTime.UtcNow)
                return BadRequest(new { message = "Reset token has expired." });

            var hasher = new PasswordHasher<SysUser>();
            user.PassWord = hasher.HashPassword(user, request.NewPassword);
            user.ResetPasswordToken = null;
            user.LastResetPasswordDate = DateTime.UtcNow;

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Password has been reset successfully." });
        }

           
        public class ForgotPasswordRequest
        {
            [Required]
            public string UsernameOrEmail { get; set; }
        }

        public class ResetPasswordWithTokenRequest
        {
            [Required]
            public string Token { get; set; }
            [Required]
            public string NewPassword { get; set; }
        }

        
    }
}
