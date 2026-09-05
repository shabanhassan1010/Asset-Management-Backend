#region
using Asset.Application.Common.Constants;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using Asset.Domain.Enum;
using Asset.Application.Interfaces.Comman;
namespace Asset.Infastructure.Service;
#endregion
public class CurrentUserService : ICurrentUserService
{
    #region Fields
    // use IHttpContextAccessor to access the current HttpContext and retrieve the ClaimsPrincipal representing the authenticated user.
    private readonly IHttpContextAccessor _httpContextAccessor;
    #endregion

    #region Constructor
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    #endregion

    #region Methods
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;  // Retrieves the ClaimsPrincipal representing the authenticated user from the current HttpContext. 
    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier); // Retrieves the user's unique identifier from the claims. which exist in token 
    public string? UserName => User?.FindFirstValue(ClaimTypes.Name);         // Retrieves the user's employee ID from the claims and attempts to parse it as an integer. which exist in token 
    public int? EmployeeId // Retrieves the user's employee ID from the claims and attempts to parse it as an integer.
    {
        get
        {
            var raw = User?.FindFirstValue(CustomClaimTypes.EmployeeId);
            return int.TryParse(raw, out var employeeId) ? employeeId : null;
        }
    }

    //public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false; // Checks if the user is authenticated by checking the IsAuthenticated property of the ClaimsPrincipal's Identity.
    public bool IsAdmin => User?.IsInRole(nameof(Role.Admin)) ?? false;      // Checks if the user is in the "Admin" role by checking if the ClaimsPrincipal has a claim of type Role with the value "Admin".
    #endregion
}