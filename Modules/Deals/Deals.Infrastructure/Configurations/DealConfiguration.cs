using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("Deals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.ClientId)
            .IsRequired();

        builder.Property(x => x.ResponsibleUserId)
            .IsRequired();

        builder.Property(x => x.StageId)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BonusPointsUsed)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.BonusDiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.FinalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.ClosedAt)
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasOne<DealStage>()
            .WithMany()
            .HasForeignKey(x => x.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey(x => x.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StageHistory)
            .WithOne()
            .HasForeignKey(x => x.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.StageHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.ClientId });
        builder.HasIndex(x => new { x.OrganizationId, x.ResponsibleUserId });
        builder.HasIndex(x => new { x.OrganizationId, x.StageId });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
    }
}
