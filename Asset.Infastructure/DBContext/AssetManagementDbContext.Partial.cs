using Microsoft.EntityFrameworkCore;
namespace Asset.Infastructure.Models;

public partial class AssetManagementDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AssetManagementDbContext).Assembly);
    }
}
