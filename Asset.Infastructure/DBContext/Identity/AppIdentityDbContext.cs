#region
using Asset.Domain.Identity;
using Asset.Infastructure.Configurations;
using Asset.Infastructure.Configurations.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
#endregion
namespace Asset.Infastructure.DBContext.Identity
{
    public class AppIdentityDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options): base(options) {}
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);  
            builder.ApplyConfigurationsFromAssembly
                (typeof(AppIdentityDbContext).Assembly,  // mean the project which contains the AppIdentityDbContext class
                type =>type.Namespace == typeof(RefreshTokensConfiguration).Namespace); // == Only Apply configuration from "Asset.Infastructure.Configurations.Identity" namespace
        }
    }
}