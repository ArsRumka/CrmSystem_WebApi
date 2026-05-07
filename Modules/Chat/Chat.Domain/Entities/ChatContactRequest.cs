using Chat.Domain.Enums;

namespace Chat.Domain.Entities;

public class ChatContactRequest
{
    public Guid Id { get; private set; }
    public Guid RequesterOrganizationId { get; private set; }
    public Guid TargetOrganizationId { get; private set; }
    public Guid RequesterUserId { get; private set; }
    public string? Message { get; private set; }
    public ChatContactRequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public Guid? RespondedByUserId { get; private set; }
    public Guid? ConversationId { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    private ChatContactRequest()
    {
    }

    public ChatContactRequest(
        Guid id,
        Guid requesterOrganizationId,
        Guid targetOrganizationId,
        Guid requesterUserId,
        string? message,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (requesterOrganizationId == Guid.Empty)
            throw new ArgumentException("RequesterOrganizationId is required", nameof(requesterOrganizationId));

        if (targetOrganizationId == Guid.Empty)
            throw new ArgumentException("TargetOrganizationId is required", nameof(targetOrganizationId));

        if (requesterOrganizationId == targetOrganizationId)
            throw new ArgumentException("Requester and target organizations must be different", nameof(targetOrganizationId));

        if (requesterUserId == Guid.Empty)
            throw new ArgumentException("RequesterUserId is required", nameof(requesterUserId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        RequesterOrganizationId = requesterOrganizationId;
        TargetOrganizationId = targetOrganizationId;
        RequesterUserId = requesterUserId;
        Message = NormalizeOptional(message, nameof(message), 1000);
        Status = ChatContactRequestStatus.Pending;
        CreatedAt = createdAt;
    }

    public void Approve(Guid conversationId, Guid respondedByUserId, DateTime respondedAt)
    {
        EnsurePending();

        if (conversationId == Guid.Empty)
            throw new ArgumentException("ConversationId is required", nameof(conversationId));

        if (respondedByUserId == Guid.Empty)
            throw new ArgumentException("RespondedByUserId is required", nameof(respondedByUserId));

        if (respondedAt == default)
            throw new ArgumentException("RespondedAt is required", nameof(respondedAt));

        Status = ChatContactRequestStatus.Approved;
        ConversationId = conversationId;
        RespondedByUserId = respondedByUserId;
        RespondedAt = respondedAt;
    }

    public void Reject(string? reason, Guid respondedByUserId, DateTime respondedAt)
    {
        EnsurePending();

        if (respondedByUserId == Guid.Empty)
            throw new ArgumentException("RespondedByUserId is required", nameof(respondedByUserId));

        if (respondedAt == default)
            throw new ArgumentException("RespondedAt is required", nameof(respondedAt));

        Status = ChatContactRequestStatus.Rejected;
        RejectionReason = NormalizeOptional(reason, nameof(reason), 1000);
        RespondedByUserId = respondedByUserId;
        RespondedAt = respondedAt;
    }

    public void Cancel(Guid cancelledByUserId, DateTime cancelledAt)
    {
        EnsurePending();

        if (cancelledByUserId == Guid.Empty)
            throw new ArgumentException("CancelledByUserId is required", nameof(cancelledByUserId));

        if (cancelledAt == default)
            throw new ArgumentException("CancelledAt is required", nameof(cancelledAt));

        Status = ChatContactRequestStatus.Cancelled;
        CancelledByUserId = cancelledByUserId;
        CancelledAt = cancelledAt;
    }

    private void EnsurePending()
    {
        if (Status != ChatContactRequestStatus.Pending)
            throw new InvalidOperationException("Only pending contact requests can be changed");
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
