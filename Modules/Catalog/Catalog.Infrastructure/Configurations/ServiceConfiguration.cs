using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.CategoryId)
            .IsRequired(false);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BonusType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.BonusValue)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.DiscountType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.DiscountValue)
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => new { x.OrganizationId, x.CategoryId });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
        builder.HasIndex(x => new { x.OrganizationId, x.BonusType });
        builder.HasIndex(x => new { x.OrganizationId, x.DiscountType });
    }
}
