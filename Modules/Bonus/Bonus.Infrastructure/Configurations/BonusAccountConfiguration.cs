using Bonus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bonus.Infrastructure.Configurations;

public sealed class BonusAccountConfiguration : IEntityTypeConfiguration<BonusAccount>
{
    public void Configure(EntityTypeBuilder<BonusAccount> builder)
    {
        builder.ToTable("BonusAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.ClientId)
            .IsRequired();

        builder.Property(x => x.Balance)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.ClientId })
            .IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}
