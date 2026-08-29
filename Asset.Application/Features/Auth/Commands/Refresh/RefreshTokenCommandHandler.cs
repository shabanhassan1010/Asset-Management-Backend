#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Auth.Commands.Refresh;
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    #region Fields
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _tokenService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    #endregion

    #region Constructor
    public RefreshTokenCommandHandler(IRefreshTokenRepository refreshTokens,
                                      ITokenHasher tokenHasher,
                                      IUserRepository users,
                                      IJwtTokenService tokenService,
                                      IIdentityUnitOfWork unitOfWork,
                                      IMapper mapper)
    {
        _refreshTokens = refreshTokens;
        _tokenHasher = tokenHasher;
        _users = users;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    #endregion

    #region Handler
    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // One message for every failure below, for the same reason as login.
        const string invalid = "The refresh token is invalid or has expired. Please sign in again.";

        var incomingHash = _tokenHasher.Hash(request.RefreshToken);
        // take this refresh Tokens which user sent it and search about it in database
        var stored = await _refreshTokens.GetByTokenHashAsync(incomingHash, cancellationToken);

        if (stored is null)
            throw new AuthenticationFailedException(invalid);

        if (stored.IsRevoked)   // If IsRevoked true => IsRevoked is cacelled so I can not use it to get new [Access token]
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);  // search for all refresh token and make IsRevoked = True
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException(invalid);
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new AuthenticationFailedException(invalid);

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException(invalid);

        // The role is re-read on every refresh, so a demotion takes effect on
        // the next refresh even though the old access token cannot be edited.
        var role = await _users.GetRoleAsync(user, cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user, role);
        var newRefreshToken = _tokenService.CreateRefreshToken();
        var newTokenHash = _tokenHasher.Hash(newRefreshToken.Token);
        // Rotation: retire the old row and point it at its replacement, so the
        // chain is walkable when a replay has to be investigated.
        stored.IsRevoked = true;
        stored.ReplacedByTokenHash = newTokenHash;

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = newRefreshToken.ExpiresAtUtc,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Both writes commit together or neither does.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = newRefreshToken.Token,
            RefreshTokenExpiresAtUtc = newRefreshToken.ExpiresAtUtc,
            User = _mapper.Map<CurrentUserDto>(new UserWithRole(user, role))
        };
    }
    #endregion
}