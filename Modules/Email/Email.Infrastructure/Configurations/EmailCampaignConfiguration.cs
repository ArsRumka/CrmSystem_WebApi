using Email.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Email.Infrastructure.Configurations;

public sealed class EmailCampaignConfiguration : IEntityTypeConfiguration<EmailCampaign>
{
    public void Configure(EntityTypeBuilder<EmailCampaign> builder)
    {
        builder.ToTable("EmailCampaigns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired(false);
        builder.Property(x => x.StartedAt).IsRequired(false);
        builder.Property(x => x.CompletedAt).IsRequired(false);
        builder.Property(x => x.TotalRecipients).IsRequired();
        builder.Property(x => x.SentCount).IsRequired();
        builder.Property(x => x.FailedCount).IsRequired();
        builder.Property(x => x.SkippedCount).IsRequired();

        builder.HasOne<EmailTemplate>()
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Recipients)
            .WithOne()
            .HasForeignKey(x => x.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Recipients)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.OrganizationId, x.Type });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        builder.HasIndex(x => new { x.OrganizationId, x.TemplateId });
    }
}
