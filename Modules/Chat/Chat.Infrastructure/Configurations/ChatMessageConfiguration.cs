using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Configurations;

public sealed class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("ChatMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ConversationId)
            .IsRequired();

        builder.Property(x => x.SenderOrganizationId)
            .IsRequired();

        builder.Property(x => x.SenderUserId)
            .IsRequired();

        builder.Property(x => x.Text)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.EditedAt)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .IsRequired(false);

        builder.Property(x => x.DeletedByUserId)
            .IsRequired(false);

        builder.Property(x => x.IsDeleted)
            .IsRequired();

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        builder.HasIndex(x => x.SenderOrganizationId);
        builder.HasIndex(x => x.SenderUserId);
        builder.HasIndex(x => x.IsDeleted);
    }
}
