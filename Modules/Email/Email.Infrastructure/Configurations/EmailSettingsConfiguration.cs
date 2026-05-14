using Email.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Email.Infrastructure.Configurations;

public sealed class EmailSettingsConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        builder.ToTable("EmailSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrganizationId).IsRequired();
        builder.Property(x => x.SenderName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SenderEmail).IsRequired().HasMaxLength(320);
        builder.Property(x => x.SmtpHost).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SmtpPort).IsRequired();
        builder.Property(x => x.UseSsl).IsRequired();
        builder.Property(x => x.Username).IsRequired().HasMaxLength(320);
        builder.Property(x => x.PasswordEncrypted).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired(false);

        builder.HasIndex(x => x.OrganizationId).IsUnique();
    }
}
