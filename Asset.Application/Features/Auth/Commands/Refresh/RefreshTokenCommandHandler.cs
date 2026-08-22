using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using MediatR;

namespace Asset.Application.Features.Auth.Commands.Refresh;

/// <summary>
/// R1.6 - refresh with rotation. The presented token is always retired and a
/// new one issued, so a stolen token is usable at most once.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IUserRepository _users;
    private readonly IJwtTokenService _tokenService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IUserRepository users,
        IJwtTokenService tokenService,
        IIdentityUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _refreshTokens = refreshTokens;
        _users = users;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // One message for every failure below, for the same reason as login.
        const string invalid = "The refresh token is invalid or has expired. Please sign in again.";

        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (stored is null)
            throw new AuthenticationFailedException(invalid);

        // A revoked token being presented means one of two things: the user
        // signed out, or somebody replayed a token that was already rotated.
        // Both are handled the same way - kill every session for that user. If
        // it really was stolen, the thief and the victim are both signed out
        // and the victim notices something is wrong.
        if (stored.IsRevoked)
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, cancellationToken);
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

        // Rotation: retire the old row and point it at its replacement, so the
        // chain is walkable when a replay has to be investigated.
        stored.IsRevoked = true;
        stored.ReplacedByToken = newRefreshToken.Token;

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshToken.Token,
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
}
