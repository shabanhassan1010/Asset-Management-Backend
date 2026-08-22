namespace Asset.Infastructure.Security
{
    public class JWTSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        /// <summary>Short, because a JWT cannot be revoked: its blast radius is its lifetime.</summary>
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
    }
}
