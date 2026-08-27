namespace Asset.Domain.Identity
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; } 
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}