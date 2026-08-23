#region
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Asset.Application.Common.Constants;
using Asset.Application.Common.Interfaces;
using Asset.Application.Features.Auth.DTOs;
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

    #region Login Function
    public AccessTokenResult CreateAccessToken(ApplicationUser user, Role role)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);  // 15 minuts

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),                      // Sub (Subject)   => related for any User?
            new(ClaimTypes.NameIdentifier, user.Id),                       // (NameIdentifier) => will make [Asp.Net Core] work with [User Id] like [Identity claim]
            new(ClaimTypes.Name, user.UserName ?? string.Empty),          // Store [User Name]
            new(ClaimTypes.Role, role.ToString()),                       // Store  [Role]                                                                         
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // this is Unique Identifier for [JWT]  => each Access Token has Different Id
        };

        if (!string.IsNullOrEmpty(user.Email))                        // If User have an [email] set this in [JWT]
            claims.Add(new Claim(ClaimTypes.Email, user.Email));


        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(CustomClaimTypes.EmployeeId, user.EmployeeId.Value.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));   // use SigningKey
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,          //  مين اصدر التوكن؟
            audience: _settings.Audience,     // الـتوكن  ده معمول لمين؟
            claims: claims,                  //  دي البيانات التي وضعتها داخل الـ JWT.    
            notBefore: DateTime.UtcNow,       // الـتوكن  يصبح صالحًا من الوقت ده.
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