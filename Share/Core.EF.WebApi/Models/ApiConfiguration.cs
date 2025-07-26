namespace Core.EF.WebApi.Models
{
    public class ApiConfiguration
    {
        public int AllowedClockSkewInMinutes { get; set; } = 5;

        public string? SecurityKey { get; set; } = "GXxi6irUYLYLPEYpJ8aZbIADxiBMOpqd";

        /// <summary>
        /// Gets or sets a value allow origin
        /// </summary>
        public string? AllowOrigins { get; set; }

        /// <summary>
        /// Gets or sets a value allow issuer
        /// </summary>
        public string? Issuer { get; set; } = "Issuer";

        /// <summary>
        /// Gets or sets a value allow audience
        /// </summary>
        public string? Audience { get; set; } = "Audience";
        public int TokenLifetime { set; get; } = 60;
        public int RefreshTokenTTL { set; get; } = 60;
        public int TokenExpiryInMinutes { set; get; } = 60;
    }
}
