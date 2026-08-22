using Asset.Application.Common.Interfaces;
using MediatR;
using Asset.Domain.Identity;
using Asset.Domain.Exceptions;
using Asset.Domain.Enum;

namespace Asset.Application.Features.Users.Commands.ChangeUserRole;

public class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand>
{
    #region Fields
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityUnitOfWork _unitOfWork;
    #endregion

    #region Constructor
    public ChangeUserRoleCommandHandler(IUserRepository users,IRefreshTokenRepository refreshTokens,
                                        ICurrentUserService currentUser,
                                        IIdentityUnitOfWork unitOfWork)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    #endregion

    #region Handlers
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
                   ?? throw new NotFoundException("That user no longer exists.");

        var currentRole = await _users.GetRoleAsync(user, cancellationToken);

        // Nothing to do. Not an error - the client just sent the state it
        // already shows, which happens when a dropdown fires on focus.
        if (currentRole == request.Role)
            return;

        // Rule 1 - no self demotion. An admin who removes their own role loses
        // access to the screen they would need to undo it.
        if (user.Id == _currentUser.UserId)
            throw new BusinessException("You cannot change your own role. Ask another administrator.");

        // Rule 2 - the system must keep at least one active admin.
        if (currentRole == Role.Admin && request.Role == Role.User)
        {
            var remainingAdmins = await _users.CountActiveAdminsExcludingAsync(user.Id, cancellationToken);
            if (remainingAdmins == 0)
                throw new BusinessException("This is the last active administrator. Promote somebody else first.");
        }

        // Rule 3 - the old sessions carry the old role inside a signed token
        // that cannot be edited. Revoking the refresh tokens means the next
        // refresh fails and the user signs in again with the new role.
        //
        // Both writes are wrapped in one transaction because UserManager saves
        // on its own: without it, a failure between the two leaves the new role
        // active with the old sessions still alive.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _users.ReplaceRoleAsync(user, currentRole, request.Role, cancellationToken);
            await _refreshTokens.RevokeAllForUserAsync(user.Id, cancellationToken);
        }, cancellationToken);
    }
    #endregion
}
