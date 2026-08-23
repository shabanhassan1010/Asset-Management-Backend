using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Common.Models;
public record UserWithRole(ApplicationUser User, Role Role);
