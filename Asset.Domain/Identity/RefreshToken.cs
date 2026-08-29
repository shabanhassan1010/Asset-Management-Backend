namespace Asset.Domain.Identity
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; } 
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}