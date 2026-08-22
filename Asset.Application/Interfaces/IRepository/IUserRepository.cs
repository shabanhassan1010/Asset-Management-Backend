using Asset.Application.Common.Models;
using Asset.Domain.Enum;
using Asset.Domain.Identity;

namespace Asset.Application.Common.Interfaces;

/// <summary>
/// Everything the features need from the user store.
///
/// The implementation wraps UserManager and AppIdentityDbContext, so
/// Microsoft.AspNetCore.Identity stays inside Infrastructure and the handlers
/// stay readable. Roles cross this boundary as the enum; the conversion to and
/// from Identity's string names happens on the other side of it.
/// </summary>
public interface IUserRepository
{
    Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);

    Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken);

    /// <summary>One role per user in this system, so this returns a single value.</summary>
    Task<Role> GetRoleAsync(ApplicationUser user, CancellationToken cancellationToken);

    /// <summary>Verifies the password against the stored hash. Never returns the hash itself.</summary>
    Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken);

    Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    /// The page and its total count in one call, because both must come from
    /// the same filter. Roles are joined in, so the list needs no N+1 lookups.
    /// </summary>
    Task<(IReadOnlyList<UserWithRole> Items, int TotalCount)> GetPagedAsync(string? search,Role? role,bool? isActive, int pageNumber,
                                                                            int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the account, hashes the password, and assigns the role.
    /// Returns Identity's error messages; an empty list means success. Returning
    /// them rather than throwing keeps the choice of status code in the handler,
    /// where the rest of the rules live.
    /// </summary>
    Task<IReadOnlyList<string>> CreateAsync(ApplicationUser user,string password,Role role,CancellationToken cancellationToken);

    Task ReplaceRoleAsync(ApplicationUser user, Role currentRole, Role newRole, CancellationToken cancellationToken);

    Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Active admins other than this one. That "other than" is what answers
    /// "would this change leave the system with zero administrators?".
    /// </summary>
    Task<int> CountActiveAdminsExcludingAsync(string excludedUserId, CancellationToken cancellationToken);
    Task<bool> EmployeeHasUserAsync(int employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<int>> GetLinkedEmployeeIdsAsync(CancellationToken cancellationToken);
}
