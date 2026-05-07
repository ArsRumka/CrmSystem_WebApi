namespace Chat.Domain.Entities;

public class ChatParticipant
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }
    public Guid? LastReadMessageId { get; private set; }
    public DateTime? LastReadAt { get; private set; }

    private ChatParticipant()
    {
    }

    public ChatParticipant(
        Guid id,
        Guid conversationId,
        Guid organizationId,
        Guid userId,
        DateTime joinedAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId is required", nameof(conversationId));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required", nameof(userId));

        if (joinedAt == default)
            throw new ArgumentException("JoinedAt is required", nameof(joinedAt));

        Id = id;
        ConversationId = conversationId;
        OrganizationId = organizationId;
        UserId = userId;
        IsActive = true;
        JoinedAt = joinedAt;
    }

    public void MarkRead(Guid? messageId, DateTime readAt)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("MessageId cannot be empty", nameof(messageId));

        if (readAt == default)
            throw new ArgumentException("ReadAt is required", nameof(readAt));

        LastReadMessageId = messageId;
        LastReadAt = readAt;
    }

    public void Deactivate(DateTime leftAt)
    {
        if (leftAt == default)
            throw new ArgumentException("LeftAt is required", nameof(leftAt));

        IsActive = false;
        LeftAt = leftAt;
    }

    public void Reactivate(DateTime joinedAt)
    {
        if (joinedAt == default)
            throw new ArgumentException("JoinedAt is required", nameof(joinedAt));

        IsActive = true;
        JoinedAt = joinedAt;
        LeftAt = null;
    }
}
