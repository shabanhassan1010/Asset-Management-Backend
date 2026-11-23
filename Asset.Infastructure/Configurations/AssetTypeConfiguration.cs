using Asset.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Asset.Infastructure.Configurations
{
    public class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
    {
        public void Configure(EntityTypeBuilder<AssetType> entity)
        {
            entity.HasIndex(e => e.TypeName, "UQ_AssetTypes_TypeName").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_AssetTypes_IsActive");

            entity.Property(e => e.TypeName).IsRequired().HasMaxLength(100);

            entity.HasIndex(a=>a.TypeName, "UX_AssetTypes_TypeName_Active")  
                  .IsUnique()
                  .HasFilter("[IsActive] = 1");

            // Arabic name (script 05 + 07). Optional, but unique among active rows when it is filled in.
            entity.Property(e => e.TypeNameAr).HasMaxLength(100);
            entity.HasIndex(a => a.TypeNameAr, "UX_AssetTypes_TypeNameAr_Active")
                  .IsUnique()
                  .HasFilter("[IsActive] = 1 AND [TypeNameAr] IS NOT NULL");
        }
    }
}
