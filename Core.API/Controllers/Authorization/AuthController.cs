using Core.EF.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Entity.Model.Systems;
using Core.API.Models;
using Core.API.Security;

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



        [NonAction]
        [HttpPost("signature")]
        [Authorize]
        public async Task<IActionResult> Signature([FromBody] SignatureRequest request)
        {          

            return Ok(new { data = Core.Helper.Helper.SignatureHelper.GenerateSignature(request.SignatureBody,request.SignatureKey) });
        }

    }
}
