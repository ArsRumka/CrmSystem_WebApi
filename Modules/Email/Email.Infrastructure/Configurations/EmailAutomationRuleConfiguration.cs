using Email.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Email.Infrastructure.Configurations;

public sealed class EmailAutomationRuleConfiguration : IEntityTypeConfiguration<EmailAutomationRule>
{
    public void Configure(EntityTypeBuilder<EmailAutomationRule> builder)
    {
        builder.ToTable("EmailAutomationRules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.TemplateId).IsRequired(false);
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.InactivityDays).IsRequired();
        builder.Property(x => x.RepeatAfterDays).IsRequired();
        builder.Property(x => x.LastRunAt).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);
        builder.Property(x => x.UpdatedByUserId).IsRequired(false);

        builder.HasOne<EmailTemplate>()
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrganizationId).IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsEnabled });
        builder.HasIndex(x => x.TemplateId);
    }
}
