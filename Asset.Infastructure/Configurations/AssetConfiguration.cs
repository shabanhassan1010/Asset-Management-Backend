using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AssetEntity = Asset.Domain.Models.Asset;

namespace Asset.Infastructure.Configurations
{
    public class AssetConfiguration : IEntityTypeConfiguration<AssetEntity>
    {
        public void Configure(EntityTypeBuilder<AssetEntity> entity)
        {
            entity.HasIndex(e => e.AssetName, "IX_Assets_AssetName");

            entity.HasIndex(e => e.AssignedEmployeeId, "IX_Assets_AssignedEmployeeId");

            entity.HasIndex(e => e.LocationId, "IX_Assets_LocationId");

            entity.HasIndex(e => new { e.Manufacturer, e.AssetTypeId }, "IX_Assets_Manufacturer_AssetTypeId");

            entity.HasIndex(e => new { e.Status, e.DepartmentId }, "IX_Assets_Status_DepartmentId");

            entity.HasIndex(e => e.AssetCode, "UQ_Assets_AssetCode").IsUnique();

            entity.HasIndex(e => e.SerialNumber, "UX_Assets_SerialNumber")
                .IsUnique()
                .HasFilter("([SerialNumber] IS NOT NULL)");

            entity.Property(e => e.AssetCode)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.AssetName)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Assets_CreatedAt");
            entity.Property(e => e.CreatedByUserId).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.Model).HasMaxLength(100);
            entity.Property(e => e.PurchaseCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RowVersion)
                .IsRequired()
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasDefaultValue(1)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Assets_Status");
            entity.Property(e => e.UpdatedByUserId).HasMaxLength(450);

            entity.HasOne(d => d.AssetType).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssetTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assets_AssetTypes");

            entity.HasOne(d => d.AssignedEmployee).WithMany(p => p.Assets)
                .HasForeignKey(d => d.AssignedEmployeeId)
                .HasConstraintName("FK_Assets_Employees");

            entity.HasOne(d => d.Category).WithMany(p => p.Assets)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Assets_Categories");

            entity.HasOne(d => d.Department).WithMany(p => p.Assets)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_Assets_Departments");

            entity.HasOne(d => d.Location).WithMany(p => p.Assets)
                .HasForeignKey(d => d.LocationId)
                .HasConstraintName("FK_Assets_Locations");
        }
    }
}
