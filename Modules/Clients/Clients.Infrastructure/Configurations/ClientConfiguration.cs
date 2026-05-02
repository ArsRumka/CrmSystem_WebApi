using Clients.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clients.Infrastructure.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.MiddleName)
            .HasMaxLength(100);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.Phone)
            .HasMaxLength(30);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.AllowMarketingEmails)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.LastName });
        builder.HasIndex(x => new { x.OrganizationId, x.Email });
        builder.HasIndex(x => new { x.OrganizationId, x.Phone });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
    }
}
