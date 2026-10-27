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

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.DepartmentName)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasAnnotation("Relational:DefaultConstraintName", "DF_Departments_IsActive");
        }
    }
}
