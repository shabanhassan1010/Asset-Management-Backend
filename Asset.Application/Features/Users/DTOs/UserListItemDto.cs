using Asset.Domain.Enum;
namespace Asset.Application.Features.Users.DTOs;
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