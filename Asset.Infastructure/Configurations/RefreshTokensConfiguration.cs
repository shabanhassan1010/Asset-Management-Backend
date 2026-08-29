using Asset.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Asset.Infastructure.Configurations
{
    public class RefreshTokensConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> entity)
        {
            entity.ToTable("RefreshTokens");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();

            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);

            entity.HasIndex(x => x.TokenHash).IsUnique();

            entity.HasOne(x => x.User).WithMany()
                                      .HasForeignKey(x => x.UserId)
                                      .OnDelete(DeleteBehavior.Cascade);
        }
    }
}