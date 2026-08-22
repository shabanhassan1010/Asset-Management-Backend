using Asset.Domain.Enum;
using Asset.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Asset.Infastructure.DBContext.Identity;

/// <summary>
/// R1.5 - seeds the roles and the demo Admin and User accounts.
///
/// Done in code rather than in a .sql script on purpose: the password hash must
/// be produced by the same hasher that will later verify it. A hash pasted into
/// a script breaks silently the day the Identity hashing defaults change.
///
/// Idempotent - it only creates what is missing, so it is safe on every start.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        // Driven by the enum, so adding a member to Role is enough to get its
        // row in AspNetRoles. The two can never drift apart.
        foreach (var role in Enum.GetValues<Role>())
            await EnsureRoleAsync(roleManager, logger, role);

        await EnsureUserAsync(userManager, logger, "admin", "admin@kinanaict.com", "Admin@123", Role.Admin , employeeId: 1);
        await EnsureUserAsync(userManager, logger, "user", "user@kinanaict.com", "User@123", Role.User , employeeId: 2);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        Role role)
    {
        var name = role.ToString();

        if (await roleManager.RoleExistsAsync(name))
            return;

        // ConcurrencyStamp is set by hand because nothing else sets it.
        //
        // IdentityUser initialises its own stamp in a property initialiser;
        // IdentityRole does not - the property is declared with no initialiser
        // at all. And RoleStore.CreateAsync only calls Context.Add(role), while
        // RoleStore.UpdateAsync is the one place that assigns a new stamp.
        //
        // The result is that a role created this way lands in AspNetRoles with
        // ConcurrencyStamp = NULL and stays that way until somebody updates it.
        // Since the schema here is hand-written SQL, that column is either
        // NOT NULL - and the insert fails - or it is nullable and optimistic
        // concurrency on roles is quietly disabled. One line closes both.
        var created = await roleManager.CreateAsync(new IdentityRole(name)
        {
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });

        // Checked rather than assumed: if role creation fails, every
        // AddToRoleAsync below fails too, and the seeded accounts end up with no
        // role at all. Better to see the reason in the log at startup.
        if (!created.Succeeded)
        {
            logger.LogError("Could not seed role {Role}: {Errors}",
                name, string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Seeded role {Role}.", name);
    }

    private static async Task EnsureUserAsync( UserManager<ApplicationUser> userManager, ILogger logger,string userName,string email,string password,Role role, int employeeId)
    {
        var existingUser = await userManager.FindByNameAsync(userName);
        if (existingUser is not null)
        {
            // Make sure the existing seeded account is linked
            // to the correct employee.
            if (existingUser.EmployeeId != employeeId)
            {
                existingUser.EmployeeId = employeeId;

                var updated = await userManager.UpdateAsync(existingUser);

                if (!updated.Succeeded)
                {
                    logger.LogError("Could not update EmployeeId for {UserName}: {Errors}",userName,
                        string.Join("; ", updated.Errors.Select(e => e.Description)));
                }
            }

            return;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // UserManager fills Id, the normalised columns, the security stamp, the
        // concurrency stamp and the password hash. Unlike roles, none of that
        // needs help here.
        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError("Could not seed {UserName}: {Errors}",
                userName, string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        var assigned = await userManager.AddToRoleAsync(user, role.ToString());
        if (!assigned.Succeeded)
        {
            // The account exists but has no role, which would make it behave as
            // a User because of the fallback in RoleExtensions. Silent failure
            // here is exactly the kind of thing that looks like a bug in the
            // authorization code later.
            logger.LogError("Seeded {UserName} but could not assign role {Role}: {Errors}",
                userName, role, string.Join("; ", assigned.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Seeded {Role} account {UserName}.", role, userName);
    }
}