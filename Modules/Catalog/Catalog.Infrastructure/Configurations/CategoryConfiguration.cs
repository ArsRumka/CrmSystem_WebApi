using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ParentCategoryId)
            .IsRequired(false);

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
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => new { x.OrganizationId, x.ParentCategoryId });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}
