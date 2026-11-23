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
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> entity)
        {
            entity.HasIndex(e => e.Code, "UQ_Departments_Code").IsUnique();

            entity.Property(e => e.Code).IsRequired().HasMaxLength(20);

            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(150);

            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Departments_IsActive");

            entity.HasIndex(a=>a.DepartmentName, "UX_Departments_DepartmentName_Active")
                  .IsUnique()
                  .HasFilter("[IsActive] = 1");

            // Arabic name (script 05 + 07). Optional, but unique among active rows when it is filled in.
            entity.Property(e => e.DepartmentNameAr).HasMaxLength(150);
            entity.HasIndex(a => a.DepartmentNameAr, "UX_Departments_DepartmentNameAr_Active")
                  .IsUnique()
                  .HasFilter("[IsActive] = 1 AND [DepartmentNameAr] IS NOT NULL");
        }
    }
}
