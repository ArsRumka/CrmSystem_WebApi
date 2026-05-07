using Bonus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bonus.Infrastructure.Configurations;

public sealed class BonusSettingsConfiguration : IEntityTypeConfiguration<BonusSettings>
{
    public void Configure(EntityTypeBuilder<BonusSettings> builder)
    {
        builder.ToTable("BonusSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.IsEnabled)
            .IsRequired();

        builder.Property(x => x.PointValue)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.AccrualType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AccrualValue)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.MaxPaymentPercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.AccrueOnBonusPayment)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganizationId)
            .IsUnique();
    }
}
