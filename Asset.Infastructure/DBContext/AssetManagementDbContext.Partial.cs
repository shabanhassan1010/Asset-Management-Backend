using Microsoft.EntityFrameworkCore;
namespace Asset.Infastructure.Models;

public partial class AssetManagementDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // Go to this assembly and see if any class excute [IEntityTypeConfiguration<T>] and exectue it
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetManagementDbContext).Assembly);
    }
}
