using Asset.Domain.Identity;
namespace Asset.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    // Use it when I Save New User In Database
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Marks every live token for the user as revoked - staged only, committed
    /// by the unit of work.
    ///
    /// Used when an account is disabled, when its role changes, and when a
    /// rotated token is replayed. Without it the user keeps their old access
    /// rights until the token expires on its own.
    /// </summary>
    Task RevokeAllForUserAsync(string userId, CancellationToken cancellationToken);
}
