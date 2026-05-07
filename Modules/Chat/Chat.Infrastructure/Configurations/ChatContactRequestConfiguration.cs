using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Configurations;

public sealed class ChatContactRequestConfiguration : IEntityTypeConfiguration<ChatContactRequest>
{
    public void Configure(EntityTypeBuilder<ChatContactRequest> builder)
    {
        builder.ToTable("ChatContactRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequesterOrganizationId)
            .IsRequired();

        builder.Property(x => x.TargetOrganizationId)
            .IsRequired();

        builder.Property(x => x.RequesterUserId)
            .IsRequired();

        builder.Property(x => x.Message)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.RespondedAt)
            .IsRequired(false);

        builder.Property(x => x.RespondedByUserId)
            .IsRequired(false);

        builder.Property(x => x.ConversationId)
            .IsRequired(false);

        builder.Property(x => x.RejectionReason)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.CancelledAt)
            .IsRequired(false);

        builder.Property(x => x.CancelledByUserId)
            .IsRequired(false);

        builder.HasOne<ChatConversation>()
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.RequesterOrganizationId);
        builder.HasIndex(x => x.TargetOrganizationId);
        builder.HasIndex(x => new { x.RequesterOrganizationId, x.TargetOrganizationId, x.Status });
        builder.HasIndex(x => new { x.TargetOrganizationId, x.Status });
        builder.HasIndex(x => x.ConversationId);
    }
}
