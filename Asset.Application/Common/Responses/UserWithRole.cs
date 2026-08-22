using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Common.Models;

/// <summary>
/// A user paired with their role.
///
/// ApplicationUser has no Roles navigation - Identity keeps the link in
/// AspNetUserRoles and IdentityUser deliberately does not expose it, so the
/// role always arrives from UserManager or from a join. This record is where
/// that result lives, so no handler carries a loose role alongside a user.
///
/// A record because it is two values glued together with no behaviour.
/// </summary>
public record UserWithRole(ApplicationUser User, Role Role);
