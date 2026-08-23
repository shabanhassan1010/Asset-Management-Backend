namespace Asset.Application.Features.Auth.DTOs
{
    public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
}
