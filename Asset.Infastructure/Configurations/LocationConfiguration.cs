using Asset.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Asset.Infastructure.Configurations
{
    public class LocationConfiguration : IEntityTypeConfiguration<Location>
    {
        public void Configure(EntityTypeBuilder<Location> entity)
        {
            entity.HasIndex(e => e.LocationName, "UQ_Locations_LocationName").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Locations_IsActive");
            entity.Property(e => e.LocationName)
                .IsRequired()
                .HasMaxLength(150);
        }
    }
}
