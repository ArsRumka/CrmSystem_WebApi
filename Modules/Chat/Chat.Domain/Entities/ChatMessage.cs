namespace Chat.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderOrganizationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Text { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public bool IsDeleted { get; private set; }

    private ChatMessage()
    {
    }

    public ChatMessage(
        Guid id,
        Guid conversationId,
        Guid senderOrganizationId,
        Guid senderUserId,
        string text,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId is required", nameof(conversationId));

        if (senderOrganizationId == Guid.Empty)
            throw new ArgumentException("SenderOrganizationId is required", nameof(senderOrganizationId));

        if (senderUserId == Guid.Empty)
            throw new ArgumentException("SenderUserId is required", nameof(senderUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        ConversationId = conversationId;
        SenderOrganizationId = senderOrganizationId;
        SenderUserId = senderUserId;
        Text = Require(text, nameof(text), 4000);
        CreatedAt = createdAt;
        IsDeleted = false;
    }

    public void Edit(string text, DateTime editedAt)
    {
        EnsureNotDeleted();

        if (editedAt == default)
            throw new ArgumentException("EditedAt is required", nameof(editedAt));

        Text = Require(text, nameof(text), 4000);
        EditedAt = editedAt;
    }

    public void SoftDelete(Guid deletedByUserId, DateTime deletedAt)
    {
        EnsureNotDeleted();

        if (deletedByUserId == Guid.Empty)
            throw new ArgumentException("DeletedByUserId is required", nameof(deletedByUserId));

        if (deletedAt == default)
            throw new ArgumentException("DeletedAt is required", nameof(deletedAt));

        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedByUserId = deletedByUserId;
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new InvalidOperationException("Deleted messages cannot be changed");
    }

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }
}
