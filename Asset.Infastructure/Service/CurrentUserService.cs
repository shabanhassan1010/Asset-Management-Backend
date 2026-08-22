#region
using Asset.Application.Common.Constants;
using Asset.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using Asset.Domain.Enum;
namespace Asset.Infastructure.Service;
#endregion

/// <summary>
/// Reads the caller off the ClaimsPrincipal that the JWT middleware built from
/// a signature-verified token. Nothing here can be forged without the key.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    #region Fields
    private readonly IHttpContextAccessor _httpContextAccessor;
    #endregion

    #region Constructor
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)  => _httpContextAccessor = httpContextAccessor;
    #endregion

    #region Methods
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);
    public int? EmployeeId
    {
        get
        {
            var raw = User?.FindFirstValue(CustomClaimTypes.EmployeeId);

            // A claim that is missing, empty, or not a number is treated as "no employee".
            // We never guess a fallback Id here - guessing would mean showing
            // one person's assets to another.
            return int.TryParse(raw, out var employeeId) ? employeeId : null;
        }
    }
    //public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) ?? false;
    #endregion
}
