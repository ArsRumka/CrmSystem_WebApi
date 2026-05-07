using Chat.Domain.Enums;

namespace Chat.Domain.Entities;

public class ChatConversation
{
    private readonly List<ChatConversationOrganization> _organizations = [];
    private readonly List<ChatParticipant> _participants = [];
    private readonly List<ChatMessage> _messages = [];

    public Guid Id { get; private set; }
    public ChatConversationType Type { get; private set; }
    public Guid OwnerOrganizationId { get; private set; }
    public string? Title { get; private set; }
    public Guid? ClientId { get; private set; }
    public Guid? DealId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public IReadOnlyCollection<ChatConversationOrganization> Organizations => _organizations;
    public IReadOnlyCollection<ChatParticipant> Participants => _participants;
    public IReadOnlyCollection<ChatMessage> Messages => _messages;

    private ChatConversation()
    {
    }

    public ChatConversation(
        Guid id,
        ChatConversationType type,
        Guid ownerOrganizationId,
        string? title,
        Guid? clientId,
        Guid? dealId,
        Guid createdByUserId,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid conversation type", nameof(type));

        if (ownerOrganizationId == Guid.Empty)
            throw new ArgumentException("OwnerOrganizationId is required", nameof(ownerOrganizationId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId is required", nameof(createdByUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId cannot be empty", nameof(clientId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty", nameof(dealId));

        if (type == ChatConversationType.Client && clientId is null)
            throw new ArgumentException("ClientId is required for client conversation", nameof(clientId));

        if (type == ChatConversationType.Deal && dealId is null)
            throw new ArgumentException("DealId is required for deal conversation", nameof(dealId));

        Id = id;
        Type = type;
        OwnerOrganizationId = ownerOrganizationId;
        Title = NormalizeOptional(title, nameof(title), 200);
        ClientId = clientId;
        DealId = dealId;
        IsActive = true;
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
    }

    public void AddOrganization(ChatConversationOrganization organization)
    {
        if (organization.ConversationId != Id)
            throw new ArgumentException("Organization membership must belong to the conversation", nameof(organization));

        if (_organizations.Any(x => x.OrganizationId == organization.OrganizationId))
            throw new InvalidOperationException("Organization is already added to conversation");

        _organizations.Add(organization);
    }

    public void AddParticipant(ChatParticipant participant)
    {
        if (participant.ConversationId != Id)
            throw new ArgumentException("Participant must belong to the conversation", nameof(participant));

        if (_participants.Any(x => x.UserId == participant.UserId))
            throw new InvalidOperationException("Participant is already added to conversation");

        _participants.Add(participant);
    }

    public void AddMessage(ChatMessage message)
    {
        if (message.ConversationId != Id)
            throw new ArgumentException("Message must belong to the conversation", nameof(message));

        _messages.Add(message);
    }

    public void UpdateTitle(string? title, DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        Title = NormalizeOptional(title, nameof(title), 200);
        UpdatedAt = updatedAt;
    }

    public void SoftDelete(Guid deletedByUserId, DateTime deletedAt)
    {
        if (deletedByUserId == Guid.Empty)
            throw new ArgumentException("DeletedByUserId is required", nameof(deletedByUserId));

        if (deletedAt == default)
            throw new ArgumentException("DeletedAt is required", nameof(deletedAt));

        IsActive = false;
        DeletedAt = deletedAt;
        DeletedByUserId = deletedByUserId;
        UpdatedAt = deletedAt;
    }

    public void EnsureActive()
    {
        if (!IsActive || DeletedAt is not null)
            throw new InvalidOperationException("Conversation is not active");
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }
}
