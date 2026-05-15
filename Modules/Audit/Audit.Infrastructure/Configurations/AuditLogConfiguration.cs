using Audit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired(false);

        builder.Property(x => x.ModuleCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.EntityId)
            .IsRequired(false);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.OldValuesJson)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.NewValuesJson)
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        builder.HasIndex(x => new { x.OrganizationId, x.ModuleCode });
        builder.HasIndex(x => new { x.OrganizationId, x.Action });
        builder.HasIndex(x => new { x.OrganizationId, x.EntityName });
        builder.HasIndex(x => new { x.OrganizationId, x.EntityId });
        builder.HasIndex(x => new { x.OrganizationId, x.UserId });
        builder.HasIndex(x => new { x.OrganizationId, x.ModuleCode, x.CreatedAt });
    }
}

