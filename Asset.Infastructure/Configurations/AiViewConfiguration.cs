using Asset.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Asset.Infastructure.Configurations
{
    public class AiViewConfiguration : IEntityTypeConfiguration<Asset.Domain.Models.vw_AssetSearch>
    {
        public void Configure(EntityTypeBuilder<vw_AssetSearch> entity)
        {
            entity
                .HasNoKey()
                .ToView("vw_AssetSearch");

            entity.Property(e => e.AssetCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.AssetName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.AssetType)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.AssignedEmployee).HasMaxLength(200);
            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.DepartmentName).HasMaxLength(150);
            entity.Property(e => e.LocationName).HasMaxLength(150);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.PurchaseCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
        }
    }
}
