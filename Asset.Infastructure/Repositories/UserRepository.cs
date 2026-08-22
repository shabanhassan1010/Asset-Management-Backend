#region
using Asset.Application.Common;
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using Asset.Infastructure.DBContext.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories;
public class UserRepository : IUserRepository
{
    #region Fields
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppIdentityDbContext _context;
    #endregion

    #region Constrcutor
    public UserRepository(UserManager<ApplicationUser> userManager, AppIdentityDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }
    #endregion

    #region Methods
    // Get
    public Task<ApplicationUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken)
         => _context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == _userManager.NormalizeName(userName), cancellationToken);
    public Task<ApplicationUser?> GetByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
    public async Task<(IReadOnlyList<UserWithRole> Items, int TotalCount)> GetPagedAsync(string? search, Role? role, bool? isActive, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        // AsNoTracking: these rows are read and mapped, never modified.
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(u =>
                u.NormalizedUserName!.Contains(term) ||
                u.NormalizedEmail!.Contains(term));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        // The role join, written out because IdentityUser exposes no navigation
        // to go through.
        var withRoles =
            from user in query
            join userRole in _context.UserRoles on user.Id equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join identityRole in _context.Roles on userRole.RoleId equals identityRole.Id into identityRoles
            from identityRole in identityRoles.DefaultIfEmpty()
            select new { User = user, RoleName = identityRole.Name };

        if (role.HasValue)
        {
            // Converted outside the expression: EF cannot translate ToString()
            // on an enum into SQL, and doing it here keeps the comparison a
            // plain string equality the provider understands.
            var roleName = role.Value.ToString();
            withRoles = withRoles.Where(x => x.RoleName == roleName);
        }

        // Counted before paging, so the total reflects the filter, not the page.
        var totalCount = await withRoles.CountAsync(cancellationToken);

        var rows = await withRoles
            // A deterministic sort is required: without one, SQL Server may
            // return the same row on two different pages.
            .OrderBy(x => x.User.UserName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new UserWithRole(x.User, x.RoleName.ToRole()))
            .ToList();

        return (items, totalCount);
    }
    public async Task<Role> GetRoleAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault().ToRole();
    }
    public async Task<IReadOnlyList<int>> GetLinkedEmployeeIdsAsync(CancellationToken cancellationToken)
    {
        return await _context.Users.AsNoTracking().Where(u => u.EmployeeId != null)
                                                  .Select(u => u.EmployeeId!.Value)
                                                  .ToListAsync(cancellationToken);
    }
    public Task<int> CountActiveAdminsExcludingAsync(string excludedUserId, CancellationToken cancellationToken)
    {
        var adminRoleName = Role.Admin.ToString();

        return (from user in _context.Users
                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                join role in _context.Roles on userRole.RoleId equals role.Id
                where user.Id != excludedUserId && user.IsActive && role.Name == adminRoleName
                select user.Id)
                .CountAsync(cancellationToken);
    }


    // Check
    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        return _userManager.CheckPasswordAsync(user, password);
    }       
    public Task<bool> UserNameExistsAsync(string userName, CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(u => u.NormalizedUserName == _userManager.NormalizeName(userName), cancellationToken);
    }
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(u => u.NormalizedEmail == _userManager.NormalizeEmail(email), cancellationToken);
    }
    public Task<bool> EmployeeHasUserAsync(int employeeId, CancellationToken cancellationToken)
    {
        return _context.Users.AnyAsync(u => u.EmployeeId == employeeId, cancellationToken);
    }


    // Create
    public async Task<IReadOnlyList<string>> CreateAsync( ApplicationUser user, string password, Role role, CancellationToken cancellationToken)
    {
        // Hashes the password, fills Id, the normalised columns and the security
        // stamp, and saves. R1.2 in one call.
        try
        {
            var created = await _userManager.CreateAsync(user, password);
            if (!created.Succeeded)
                return created.Errors.Select(e => e.Description).ToList();
        }
        catch
        {
            return new[] { "This employee already has a user account." };
        }

        var assigned = await _userManager.AddToRoleAsync(user, role.ToString());
        if (!assigned.Succeeded)
            return assigned.Errors.Select(e => e.Description).ToList();

        return Array.Empty<string>();
    }


    // Update
    public async Task ReplaceRoleAsync(ApplicationUser user, Role currentRole, Role newRole, CancellationToken cancellationToken)
    {
        await _userManager.RemoveFromRoleAsync(user, currentRole.ToString());
        await _userManager.AddToRoleAsync(user, newRole.ToString());
    }
    public Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => _userManager.UpdateAsync(user);
    #endregion
}
