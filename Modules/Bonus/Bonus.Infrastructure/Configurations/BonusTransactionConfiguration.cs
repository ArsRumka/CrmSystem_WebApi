using Bonus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bonus.Infrastructure.Configurations;

public sealed class BonusTransactionConfiguration : IEntityTypeConfiguration<BonusTransaction>
{
    public void Configure(EntityTypeBuilder<BonusTransaction> builder)
    {
        builder.ToTable("BonusTransactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.BonusAccountId)
            .IsRequired();

        builder.Property(x => x.ClientId)
            .IsRequired();

        builder.Property(x => x.DealId)
            .IsRequired(false);

        builder.Property(x => x.SourceReturnId)
            .IsRequired(false);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Points)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.MonetaryAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.PointValueAtMoment)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BalanceBefore)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.BalanceAfter)
            .HasPrecision(18, 3)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .IsRequired(false);

        builder.HasOne<BonusAccount>()
            .WithMany()
            .HasForeignKey(x => x.BonusAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.BonusAccountId });
        builder.HasIndex(x => new { x.OrganizationId, x.ClientId });
        builder.HasIndex(x => new { x.OrganizationId, x.DealId });
        builder.HasIndex(x => new { x.OrganizationId, x.DealId, x.SourceReturnId, x.Type });
        builder.HasIndex(x => new { x.OrganizationId, x.Type });
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
    }
}
