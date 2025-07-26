using Core.EF.Infrastructure.Database;
using Microsoft.IdentityModel.Tokens;
using Core.Entity.Model.Systems;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Core.API.Security
{

    public class JwtService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public JwtService(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<(string accessToken, DateTime accessTokenExpires, string refreshToken, DateTime refreshTokenExpires)> GenerateTokens(SysUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var accessTokenExpiresInMinutes = int.Parse(jwtSettings["AccessTokenExpiresInMinutes"]??"15");
            var refreshTokenExpiresInDays = int.Parse(jwtSettings["RefreshTokenExpiresInDays"] ?? "60");

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.NameId, user.ID),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim(JwtRegisteredClaimNames.GivenName, user.NickName??$"{user.FirstName} {user.LastName}"),
              //  new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var accessTokenExpires = DateTime.UtcNow.AddMinutes(accessTokenExpiresInMinutes);
            var accessToken = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: accessTokenExpires,
                signingCredentials: creds
            );

            var refreshTokenExpires = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);
            var refreshToken = Guid.NewGuid().ToString();

            // Lưu Refresh Token vào cơ sở dữ liệu
            user.JwtRefreshToken = refreshToken;
            user.LastUpdateDate = DateTime.UtcNow;
            _context.Set<SysUser>().Update(user);
             await  _context.SaveChangesAsync();

            return (
                new JwtSecurityTokenHandler().WriteToken(accessToken),
                accessTokenExpires,
                refreshToken,
                refreshTokenExpires
            );
        }

        public SysUser? ValidateUserCredentials(string username, string password)
        {
            var user = _context.Set<SysUser>().SingleOrDefault(u => u.UserName == username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PassWord))
            {
                return user;
            }
            return null;
        }
    }
}
