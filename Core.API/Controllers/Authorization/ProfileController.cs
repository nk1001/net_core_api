using Core.API.Security;
using Core.EF.Infrastructure.Database;
using Core.EF.WebApi.Authorize;
using Core.Entity.Model.Systems;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations; // For PasswordHasher
using System.Security.Claims;

namespace Core.API.Controllers.Authorization
{
    [Route("api/auth")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
     
        private readonly JwtService _jwtService;
        private readonly AppDbContext _dbContext;

        public ProfileController(JwtService jwtService, AppDbContext dbContext)
        {
            _jwtService = jwtService;
            _dbContext = dbContext;
        }
        
        [HttpPost("reset-password")]
        [Authorize]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordWithOldRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.ID == userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Verify old password
            var hasher = new PasswordHasher<SysUser>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PassWord ?? "", request.OldPassword);
            if (verifyResult == PasswordVerificationResult.Failed)
                return BadRequest(new { message = "Old password is incorrect" });

            // Set new password
            user.PassWord = hasher.HashPassword(user, request.NewPassword);
            user.LastResetPasswordDate = DateTime.UtcNow;

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Password reset successful" });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.ID == userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Return all fields except tokens, now including Status
            return Ok(new
            {   
                user.ID,
                user.UserName,
                user.LoginIp,
                user.NickName,
                user.LastName,
                user.FirstName,
                user.PhotoUrl,
                user.LastLoginDate,
                user.Birthday,
                user.Sex,
                user.Address,
                user.Phone,
                user.Email,
                user.LastResetPasswordDate,
                user.Roles,
                user.Status // <-- Added status
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileWithFileRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _dbContext.Set<SysUser>().FirstOrDefaultAsync(u => u.ID == userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Update all fields except tokens and password          
            user.NickName = request.NickName;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.LastLoginDate = request.LastLoginDate;
            user.Birthday = request.Birthday;
            user.Sex = request.Sex;
            user.Address = request.Address;
            user.Phone = request.Phone;
            user.Email = request.Email;
            user.Roles = request.Roles;
            user.LastUpdateDate = DateTime.UtcNow;
            user.Status = request.Status ?? user.Status; // <-- Update status if provided

            // Handle file upload
            if (request.Photo != null && request.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileExt = Path.GetExtension(request.Photo.FileName);
                var fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}{fileExt}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.Photo.CopyToAsync(stream);
                }

                // Set PhotoUrl as a relative path or URL
                user.PhotoUrl = $"/uploads/avatars/{fileName}";
            }

            _dbContext.Set<SysUser>().Update(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully", photoUrl = user.PhotoUrl, status = user.Status });
        }

        
        // Request model for reset-password with old password check
        public class ResetPasswordWithOldRequest
        {
            [Required]
            public string OldPassword { get; set; }
            [Required]
            public string NewPassword { get; set; }
        }

       
        // UpdateProfileWithFileRequest for multipart/form-data
        public class UpdateProfileWithFileRequest
        {
            public string? NickName { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTime? LastLoginDate { get; set; }
            public DateTime? Birthday { get; set; }
            public int? Sex { get; set; }
            public string? Address { get; set; }
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public List<string>? Roles { get; set; }
            public IFormFile? Photo { get; set; }
            public int? Status { get; set; } // 1 = active, 2 = inactive, 3 = remove
        }
    }
}
