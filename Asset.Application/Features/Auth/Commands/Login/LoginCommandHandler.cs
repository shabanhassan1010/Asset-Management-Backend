using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using MediatR;

namespace Asset.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _tokenService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LoginCommandHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenService tokenService,
        IIdentityUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByUserNameAsync(request.UserName, cancellationToken);

        // The same message for "no such user" and "wrong password. Two different messages would turn this endpoint into a username oracle.
        const string badCredentials = "The username or password is incorrect.";

        if (user is null)
            throw new AuthenticationFailedException(badCredentials);

        if (!await _users.CheckPasswordAsync(user, request.Password, cancellationToken))
            throw new AuthenticationFailedException(badCredentials);

        // Checked AFTER the password on purpose. Telling an anonymous caller
        // "that account is disabled" before they prove the password confirms
        // the account exists.
        if (!user.IsActive)
            throw new AuthenticationFailedException("This account has been disabled by an administrator.");

        var role = await _users.GetRoleAsync(user, cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user, role);
        var refreshToken = _tokenService.CreateRefreshToken();

        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAtUtc,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = _mapper.Map<CurrentUserDto>(new UserWithRole(user, role))
        };
    }
}
