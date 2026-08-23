#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    #region Fields
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _tokenService;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    #endregion

    #region Constructor
    public LoginCommandHandler(IUserRepository users,IRefreshTokenRepository refreshTokens, IJwtTokenService tokenService,
                               IIdentityUnitOfWork unitOfWork, IMapper mapper)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    #endregion

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        const string badCredentials = "The username or password is incorrect.";
        var user = await _users.GetByUserNameAsync(request.UserName, cancellationToken);

        if (user is null)
            throw new AuthenticationFailedException(badCredentials);

        if (!await _users.CheckPasswordAsync(user, request.Password, cancellationToken))
            throw new AuthenticationFailedException(badCredentials);

        if (!user.IsActive)
            throw new AuthenticationFailedException("This account has been disabled by an administrator.");

        var role = await _users.GetRoleAsync(user, cancellationToken);

        var accessToken = _tokenService.CreateAccessToken(user, role);
        var refreshToken = _tokenService.CreateRefreshToken();

        // after sent Refresh Token into Frontend will Save Refresh Token in Database
        await _refreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAtUtc,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // before this step Refresh Token exists in EF tracking
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Token,                          // use it with Api Resquest
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,       // Angular with it know if token is expire or not
            RefreshToken = refreshToken.Token,                       //  use it to get a new Access Token
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,     
            User = _mapper.Map<CurrentUserDto>(new UserWithRole(user, role))
        };
    }
}