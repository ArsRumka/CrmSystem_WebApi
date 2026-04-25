using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Configurations;

public class ActivationKeyConfiguration : IEntityTypeConfiguration<ActivationKey>
{
    public void Configure(EntityTypeBuilder<ActivationKey> builder)
    {
        builder.ToTable("ActivationKeys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.KeyHash)
            .IsRequired();

        builder.Property(x => x.IsUsed)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasIndex(x => x.KeyHash)
            .IsUnique();

        builder.HasIndex(x => x.OrganizationRequestId);

        builder.HasOne<OrganizationRequest>()
            .WithMany()
            .HasForeignKey(x => x.OrganizationRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SystemAdmin>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBySystemAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
