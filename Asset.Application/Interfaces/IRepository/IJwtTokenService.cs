#region
using Asset.Application.Features.Auth.DTOs;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
#endregion

namespace Asset.Application.Common.Interfaces;
public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(ApplicationUser user, Role role);
    RefreshTokenResult CreateRefreshToken();
}