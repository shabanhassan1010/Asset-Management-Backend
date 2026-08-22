#region
using Asset.Application.Common.Interfaces;
using MediatR;
using Asset.Domain.Identity;
using Asset.Domain.Enum;
using Asset.Domain.Exceptions;
#endregion

namespace Asset.Application.Features.Users.Commands.ChangeUserStatus;

public class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand>
{
    #region Fields
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    #endregion

    #region
    public ChangeUserStatusCommandHandler(IUserRepository users,IRefreshTokenRepository refreshTokens,ICurrentUserService currentUser,IIdentityUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    #endregion

    #region Handlers
    public async Task Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("That user no longer exists.");

        if (user.IsActive == request.IsActive)
            return;

        // Only disabling can lock somebody out, so both guards sit on that side.
        if (!request.IsActive)
        {
            if (user.Id == _currentUser.UserId)
                throw new BusinessException("You cannot disable your own account.");

            var role = await _users.GetRoleAsync(user, cancellationToken);
            if (role == Role.Admin)
            {
                var remainingAdmins = await _users.CountActiveAdminsExcludingAsync(user.Id, cancellationToken);
                if (remainingAdmins == 0)
                    throw new BusinessException("This is the last active administrator. Promote somebody else first.");
            }
        }

        user.IsActive = request.IsActive;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _users.UpdateAsync(user, cancellationToken);

            // A disabled account must stop working now, not when its token
            // happens to expire.
            if (!request.IsActive)
                await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        }, cancellationToken);
        #endregion
    }
}