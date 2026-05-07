using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealReturnConfiguration : IEntityTypeConfiguration<DealReturn>
{
    public void Configure(EntityTypeBuilder<DealReturn> builder)
    {
        builder.ToTable("DealReturns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.DealId)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BonusPointsReturned)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.BonusAccrualReversed)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.MoneyAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.CompletedAt)
            .IsRequired(false);

        builder.Property(x => x.CompletedByUserId)
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .IsRequired(false);

        builder.Property(x => x.CancelledByUserId)
            .IsRequired(false);

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasOne<Deal>()
            .WithMany()
            .HasForeignKey(x => x.DealId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.DealReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.DealId });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
    }
}
