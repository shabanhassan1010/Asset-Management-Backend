using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Features.Users.DTOs;

/// <summary>
/// One row of the Users screen.
///
/// ApplicationUser carries no display name, so the UI identifies an account by
/// username and email - which is what an administrator recognises anyway.
/// PasswordHash and SecurityStamp are absent by construction: the entity is
/// never returned as the API contract.
/// </summary>
public class UserListItemDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Role Role { get; set; }
    public bool IsActive { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
}
