using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Core.EF.WebApi
{
    public class JwtSecurityTokenHandlerCustom : ISecurityTokenValidator
    {
        private readonly System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler _tokenHandler;
        public JwtSecurityTokenHandlerCustom()
        {
            _tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        }

        public bool CanReadToken(string securityToken)
        {
            return _tokenHandler.CanReadToken(securityToken);
        }

        public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters,
            out SecurityToken validatedToken)
        {
            var principal = _tokenHandler.ValidateToken(securityToken, validationParameters, out validatedToken);

            if (principal == null)
            {
                throw new SecurityTokenException("invalid token");
            }

           

            return principal;
        }

        public bool CanValidateToken { get; } = true;
        public int MaximumTokenSizeInBytes { get; set; } = TokenValidationParameters.DefaultMaximumTokenSizeInBytes;
    }

    public class TokenPayLoad
    {
        public string uid { get; set; }
        public string email { get; set; }
        public string fullName { get; set; }
    }
}
