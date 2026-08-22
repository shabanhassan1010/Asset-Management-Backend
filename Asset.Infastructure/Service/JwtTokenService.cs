#region
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Asset.Application.Common.Constants;
using Asset.Application.Common.Interfaces;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using Asset.Infastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
#endregion

namespace Asset.Infastructure.Service;
public class JwtTokenService : IJwtTokenService
{
    #region Fields
    private readonly JWTSettings _settings;
    #endregion

    #region Constructor
    public JwtTokenService(IOptions<JWTSettings> settings)
    {
        _settings = settings.Value;
    }
    #endregion

    #region Functions
    public AccessTokenResult CreateAccessToken(ApplicationUser user, Role role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id), 
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        // The bridge to Employees. It has to travel in the token because
        // Employees lives in the other DbContext - EF cannot Include across
        // that boundary, so carrying the id here saves a cross-context query on
        // every "show me my assets" request (R4).
        //if (user.EmployeeId.HasValue)
        //    claims.Add(new Claim("employeeId", user.EmployeeId.Value.ToString()));

        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(CustomClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        // Cryptographically random, not a Guid. A Guid is unique but not
        // unpredictable, and this value is a credential.
        var bytes = RandomNumberGenerator.GetBytes(64);

        return new RefreshTokenResult( Convert.ToBase64String(bytes), DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }
    #endregion
}