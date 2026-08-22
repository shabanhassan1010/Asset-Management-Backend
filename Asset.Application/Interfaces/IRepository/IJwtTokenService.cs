using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Common.Interfaces;

/// <summary>
/// Token minting behind an interface, so the application layer never
/// references System.IdentityModel and the service can be faked in tests.
/// </summary>
public interface IJwtTokenService
{
    /// <param name="role">
    /// Passed in rather than read off the user: ApplicationUser has no Roles
    /// navigation, and the caller has already resolved it.
    /// </param>
    AccessTokenResult CreateAccessToken(ApplicationUser user, Role role);

    RefreshTokenResult CreateRefreshToken();
}

/// <summary>
/// A record, not a class: an immutable pair of values with no behaviour,
/// produced in one place and read in another.
/// </summary>
public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);

public record RefreshTokenResult(string Token, DateTime ExpiresAtUtc);
