#region
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#endregion

namespace Asset.Infastructure.DBContext.Identity;
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));

        foreach (var role in Enum.GetValues<Role>())
            await EnsureRoleAsync(roleManager, logger, role);

        await EnsureUserAsync(userManager, logger, "admin", "admin@kinanaict.com", "Admin@123", Role.Admin , employeeId: 1);
        await EnsureUserAsync(userManager, logger, "user", "user@kinanaict.com", "User@123", Role.User , employeeId: 2);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager,ILogger logger,Role role)
    {
        var name = role.ToString();

        if (await roleManager.RoleExistsAsync(name))
            return;

        var created = await roleManager.CreateAsync(new IdentityRole(name)
        {
            ConcurrencyStamp = Guid.NewGuid().ToString()
        });

        if (!created.Succeeded)
        {
            logger.LogError("Could not seed role {Role}: {Errors}",name, string.Join("; ", created.Errors.Select(e => e.Description)));
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
                    logger.LogError("Could not update EmployeeId for {UserName}: {Errors}",userName, string.Join("; ", updated.Errors.Select(e => e.Description)));
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

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            logger.LogError("Could not seed {UserName}: {Errors}", userName, string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        var assigned = await userManager.AddToRoleAsync(user, role.ToString());
        if (!assigned.Succeeded)
        {
            logger.LogError("Seeded {UserName} but could not assign role {Role}: {Errors}", userName, role, string.Join("; ", assigned.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation("Seeded {Role} account {UserName}.", role, userName);
    }
}