using Email.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Email.Infrastructure.Configurations;

public sealed class EmailCampaignRecipientConfiguration : IEntityTypeConfiguration<EmailCampaignRecipient>
{
    public void Configure(EntityTypeBuilder<EmailCampaignRecipient> builder)
    {
        builder.ToTable("EmailCampaignRecipients");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.CampaignId).IsRequired();
        builder.Property(x => x.ClientId).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired(false);
        builder.Property(x => x.FullNameSnapshot).HasMaxLength(300).IsRequired(false);
        builder.Property(x => x.LastDealDate).IsRequired(false);
        builder.Property(x => x.DaysSinceLastDeal).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000).IsRequired(false);
        builder.Property(x => x.SentAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.CampaignId);
        builder.HasIndex(x => new { x.OrganizationId, x.ClientId });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });
        builder.HasIndex(x => new { x.OrganizationId, x.CampaignId, x.ClientId });
        builder.HasIndex(x => new { x.OrganizationId, x.SentAt });
    }
}
