using Asset.Domain.Enum;
namespace Asset.Application.Features.Auth.DTOs;
public class CurrentUserDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public Role Role { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; init; }
}
