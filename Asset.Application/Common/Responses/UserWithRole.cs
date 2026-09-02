using Asset.Application.Features.Employees.DTos;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using Asset.Domain.Models;

namespace Asset.Application.Common.Models;
public record UserWithRole(ApplicationUser User, Role Role , EmployeeInfo? Employee = null);
