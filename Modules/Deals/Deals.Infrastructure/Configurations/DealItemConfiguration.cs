using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealItemConfiguration : IEntityTypeConfiguration<DealItem>
{
    public void Configure(EntityTypeBuilder<DealItem> builder)
    {
        builder.ToTable("DealItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.DealId)
            .IsRequired();

        builder.Property(x => x.ItemType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ItemId)
            .IsRequired();

        builder.Property(x => x.StorageId)
            .IsRequired(false);

        builder.Property(x => x.NameSnapshot)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.PriceAtMoment)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DiscountType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.DiscountValue)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.FinalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.DealId });
        builder.HasIndex(x => new { x.OrganizationId, x.ItemType, x.ItemId });
        builder.HasIndex(x => new { x.OrganizationId, x.StorageId });
    }
}
