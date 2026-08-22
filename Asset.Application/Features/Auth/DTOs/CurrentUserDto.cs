using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Features.Auth.DTOs;

/// <summary>
/// Who the caller is, as the API sees them. Returned by GET /api/auth/me and
/// nested in the login response, so the Angular shell can render itself
/// without a second round trip.
/// </summary>
public class CurrentUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Role Role { get; set; }

    /// <summary>Null for an account with no person behind it.</summary>
    public int? EmployeeId { get; set; }
}
