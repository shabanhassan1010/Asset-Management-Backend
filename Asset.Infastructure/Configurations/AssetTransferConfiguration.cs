using Asset.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Asset.Infastructure.Configurations
{
    public class AssetTransferConfiguration : IEntityTypeConfiguration<AssetTransfer>
    {
        public void Configure(EntityTypeBuilder<AssetTransfer> entity)
        {
            entity.HasIndex(e => new { e.AssetId, e.TransferDate }, "IX_AssetTransfers_AssetId_TransferDate").IsDescending(false, true);

            entity.HasIndex(e => e.TransferredByUserId, "IX_AssetTransfers_TransferredByUserId");

            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(500);
            entity.Property(e => e.TransferDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasAnnotation("Relational:DefaultConstraintName", "DF_AssetTransfers_TransferDate");
            entity.Property(e => e.TransferredByUserId).IsRequired();

            entity.HasOne(d => d.Asset).WithMany(p => p.AssetTransfers)
                .HasForeignKey(d => d.AssetId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AssetTransfers_Assets");

            entity.HasOne(d => d.FromDepartment).WithMany(p => p.AssetTransferFromDepartments)
                .HasForeignKey(d => d.FromDepartmentId)
                .HasConstraintName("FK_AssetTransfers_FromDepartment");

            entity.HasOne(d => d.FromEmployee).WithMany(p => p.AssetTransferFromEmployees)
                .HasForeignKey(d => d.FromEmployeeId)
                .HasConstraintName("FK_AssetTransfers_FromEmployee");

            entity.HasOne(d => d.FromLocation).WithMany(p => p.AssetTransferFromLocations)
                .HasForeignKey(d => d.FromLocationId)
                .HasConstraintName("FK_AssetTransfers_FromLocation");

            entity.HasOne(d => d.ToDepartment).WithMany(p => p.AssetTransferToDepartments)
                .HasForeignKey(d => d.ToDepartmentId)
                .HasConstraintName("FK_AssetTransfers_ToDepartment");

            entity.HasOne(d => d.ToEmployee).WithMany(p => p.AssetTransferToEmployees)
                .HasForeignKey(d => d.ToEmployeeId)
                .HasConstraintName("FK_AssetTransfers_ToEmployee");

            entity.HasOne(d => d.ToLocation).WithMany(p => p.AssetTransferToLocations)
                .HasForeignKey(d => d.ToLocationId)
                .HasConstraintName("FK_AssetTransfers_ToLocation");
        }
    }
}
