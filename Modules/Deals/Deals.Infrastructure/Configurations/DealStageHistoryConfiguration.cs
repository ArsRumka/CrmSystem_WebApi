using Deals.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Deals.Infrastructure.Configurations;

public sealed class DealStageHistoryConfiguration : IEntityTypeConfiguration<DealStageHistory>
{
    public void Configure(EntityTypeBuilder<DealStageHistory> builder)
    {
        builder.ToTable("DealStageHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.DealId)
            .IsRequired();

        builder.Property(x => x.OldStageId)
            .IsRequired(false);

        builder.Property(x => x.NewStageId)
            .IsRequired();

        builder.Property(x => x.ChangedByUserId)
            .IsRequired();

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.HasOne<DealStage>()
            .WithMany()
            .HasForeignKey(x => x.OldStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DealStage>()
            .WithMany()
            .HasForeignKey(x => x.NewStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.DealId });
        builder.HasIndex(x => new { x.OrganizationId, x.ChangedAt });
    }
}
