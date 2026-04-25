using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

public class OrganizationRequestConfiguration : IEntityTypeConfiguration<OrganizationRequest>
{
    public void Configure(EntityTypeBuilder<OrganizationRequest> builder)
    {
        builder.ToTable("OrganizationRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ContactName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ContactEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.ContactPhone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Comment)
            .HasMaxLength(1000);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion(
                status => status.ToString(),
                value => Enum.Parse<OrganizationRequestStatus>(value))
            .HasMaxLength(32);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.ContactEmail);

        builder.HasIndex(x => x.Status);

        builder.HasOne<SystemAdmin>()
            .WithMany()
            .HasForeignKey(x => x.ProcessedBySystemAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
