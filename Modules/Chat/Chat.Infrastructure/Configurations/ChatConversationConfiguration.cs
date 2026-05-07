using Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chat.Infrastructure.Configurations;

public sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("ChatConversations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.OwnerOrganizationId)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.ClientId)
            .IsRequired(false);

        builder.Property(x => x.DealId)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.DeletedAt)
            .IsRequired(false);

        builder.Property(x => x.DeletedByUserId)
            .IsRequired(false);

        builder.HasMany(x => x.Organizations)
            .WithOne()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Organizations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.OwnerOrganizationId);
        builder.HasIndex(x => new { x.OwnerOrganizationId, x.Type });
        builder.HasIndex(x => new { x.OwnerOrganizationId, x.ClientId });
        builder.HasIndex(x => new { x.OwnerOrganizationId, x.DealId });
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.CreatedAt);
    }
}
