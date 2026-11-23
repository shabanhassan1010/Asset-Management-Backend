using Asset.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Infastructure.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> entity)
        {
            entity.HasIndex(e => e.CategoryName, "UQ_Categories_CategoryName").IsUnique();

            entity.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);

            entity.Property(e => e.Description).HasMaxLength(500);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Categories_IsActive");

            entity.HasIndex(x => x.CategoryName, "UX_Categories_CategoryName_Active")
                  .IsUnique()
                  .HasFilter("[IsActive] = 1");

            // Arabic name (script 05 + 07). Optional, but unique among active rows when it is filled in.
            entity.Property(e => e.CategoryNameAr).HasMaxLength(100);
            entity.HasIndex(x => x.CategoryNameAr, "UX_Categories_CategoryNameAr_Active")
                  .IsUnique()
                  .HasFilter("[IsActive] = 1 AND [CategoryNameAr] IS NOT NULL");
        }
    }
}
