using Asset.Infastructure.Configurations;
using Microsoft.EntityFrameworkCore;
namespace Asset.Infastructure.Models;

public partial class AssetManagementDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Go to this assembly and see if any class excute [IEntityTypeConfiguration<T>] and exectue it
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetManagementDbContext).Assembly,
            type=>type.Namespace == typeof(AssetConfiguration).Namespace);  // Only Apply configuration from "Asset.Infastructure.Configurations" namespace
    }
}
