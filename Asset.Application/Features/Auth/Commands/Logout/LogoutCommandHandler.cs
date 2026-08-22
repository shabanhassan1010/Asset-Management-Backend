using Asset.Application.Common.Interfaces;
using MediatR;

namespace Asset.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Revokes the refresh token the caller presents - this device only, so
/// signing out on a laptop does not sign the same person out on their phone.
///
/// A JWT cannot be un-issued, so the access token stays valid until it
/// expires. That is why its lifetime is minutes, not days.
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public LogoutCommandHandler(
        IRefreshTokenRepository refreshTokens,
        ICurrentUserService currentUser,
        IIdentityUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        // Deliberately silent when the token is missing, already revoked, or
        // belongs to somebody else. Logout is idempotent, and a 404 here would
        // let a caller probe which token values exist.
        if (stored is null || stored.IsRevoked || stored.UserId != _currentUser.UserId)
            return;

        stored.IsRevoked = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
