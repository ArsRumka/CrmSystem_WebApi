using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Configurations;

public sealed class ChatConversationOrganizationConfiguration
    : IEntityTypeConfiguration<ChatConversationOrganization>
{
    public void Configure(EntityTypeBuilder<ChatConversationOrganization> builder)
    {
        builder.ToTable("ChatConversationOrganizations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.Property(x => x.LeftAt)
            .IsRequired(false);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => new { x.ConversationId, x.OrganizationId })
            .IsUnique();
        builder.HasIndex(x => new { x.OrganizationId, x.IsActive });
    }
}
