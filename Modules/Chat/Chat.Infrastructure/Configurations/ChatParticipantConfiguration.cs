using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Configurations;

public sealed class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
{
    public void Configure(EntityTypeBuilder<ChatParticipant> builder)
    {
        builder.ToTable("ChatParticipants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.OrganizationId)
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.JoinedAt)
            .IsRequired();

        builder.Property(x => x.LeftAt)
            .IsRequired(false);

        builder.Property(x => x.LastReadMessageId)
            .IsRequired(false);

        builder.Property(x => x.LastReadAt)
            .IsRequired(false);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.OrganizationId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique();
        builder.HasIndex(x => new { x.ConversationId, x.OrganizationId });
        builder.HasIndex(x => new { x.OrganizationId, x.UserId });
        builder.HasIndex(x => x.IsActive);
    }
}
