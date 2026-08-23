#region
using Asset.Application.Common.Interfaces;
using MediatR;
#endregion

namespace Asset.Application.Features.Auth.Commands.Logout;
public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    #region Fields
    private readonly IRefreshTokenRepository _refreshTokens;  // Get refresh Tokens from database
    private readonly ICurrentUserService _currentUser;       //  know which user did this request
    private readonly IIdentityUnitOfWork _unitOfWork;  
    #endregion

    #region Constructor
    public LogoutCommandHandler(IRefreshTokenRepository refreshTokens,ICurrentUserService currentUser,IIdentityUnitOfWork unitOfWork)
    {
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    #endregion

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Get refresh Tokens from database
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        // stored.IsRevoked: Mean Token is exists but he is cancelled
        if (stored is null || stored.IsRevoked || stored.UserId != _currentUser.UserId)
            return;

        stored.IsRevoked = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
