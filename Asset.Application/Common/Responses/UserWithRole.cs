using Asset.Application.Features.Employees.DTos;
using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Common.Models;

// use this class in User profile response to include the role and employee information
public record UserWithRole(ApplicationUser User, Role Role , EmployeeInfo? Employee = null);