using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealReturnItemConfiguration : IEntityTypeConfiguration<DealReturnItem>
{
    public void Configure(EntityTypeBuilder<DealReturnItem> builder)
    {
        builder.ToTable("DealReturnItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.DealReturnId)
            .IsRequired();

        builder.Property(x => x.DealId)
            .IsRequired();

        builder.Property(x => x.DealItemId)
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
            .HasMaxLength(300);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.ReturnAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne<DealItem>()
            .WithMany()
            .HasForeignKey(x => x.DealItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.DealReturnId });
        builder.HasIndex(x => new { x.OrganizationId, x.DealId });
        builder.HasIndex(x => new { x.OrganizationId, x.DealItemId });
        builder.HasIndex(x => new { x.OrganizationId, x.ItemId });
    }
}
