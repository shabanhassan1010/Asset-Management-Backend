namespace Asset.Application.Features.Auth.DTOs;

/// <summary>
/// What login and refresh both return.
///
/// The expiry times are explicit so the client can refresh just before the
/// access token dies, instead of waiting for a 401 and retrying.
/// </summary>
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    public CurrentUserDto User { get; set; } = new();
}
