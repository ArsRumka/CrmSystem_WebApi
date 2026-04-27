using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealStageConfiguration : IEntityTypeConfiguration<DealStage>
{
    public void Configure(EntityTypeBuilder<DealStage> builder)
    {
        builder.ToTable("DealStages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Order)
            .IsRequired();

        builder.Property(x => x.IsFinal)
            .IsRequired();

        builder.Property(x => x.IsSuccessful)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Name });
        builder.HasIndex(x => new { x.OrganizationId, x.Order });
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}
