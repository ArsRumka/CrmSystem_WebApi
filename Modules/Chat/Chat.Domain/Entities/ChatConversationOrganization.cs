namespace Chat.Domain.Entities;

public class ChatConversationOrganization
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    private ChatConversationOrganization()
    {
    }

    public ChatConversationOrganization(
        Guid id,
        Guid conversationId,
        Guid organizationId,
        DateTime joinedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId is required", nameof(conversationId));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (joinedAt == default)
            throw new ArgumentException("JoinedAt is required", nameof(joinedAt));

        Id = id;
        ConversationId = conversationId;
        OrganizationId = organizationId;
        IsActive = true;
        JoinedAt = joinedAt;
    }

    public void Deactivate(DateTime leftAt)
    {
        if (leftAt == default)
            throw new ArgumentException("LeftAt is required", nameof(leftAt));

        IsActive = false;
        LeftAt = leftAt;
    }
}
