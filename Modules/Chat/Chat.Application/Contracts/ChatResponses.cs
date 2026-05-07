using Chat.Domain.Enums;

namespace Chat.Application.Contracts;

public sealed record ChatConversationResponse(
    Guid Id,
    ChatConversationType Type,
    Guid OwnerOrganizationId,
    string? Title,
    Guid? ClientId,
    Guid? DealId,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    DateTime? DeletedAt,
    IReadOnlyList<ChatConversationOrganizationResponse> Organizations,
    IReadOnlyList<ChatParticipantResponse> Participants,
    ChatMessageResponse? LastMessage,
    DateTime? LastMessageAt,
    int UnreadCount);

public sealed record ChatConversationOrganizationResponse(
    Guid OrganizationId,
    string? OrganizationName,
    bool IsActive,
    DateTime JoinedAt,
    DateTime? LeftAt);

public sealed record ChatParticipantResponse(
    Guid Id,
    Guid ConversationId,
    Guid OrganizationId,
    string? OrganizationName,
    Guid UserId,
    string? UserDisplayName,
    bool IsActive,
    DateTime JoinedAt,
    DateTime? LeftAt,
    Guid? LastReadMessageId,
    DateTime? LastReadAt);

public sealed record ChatMessageResponse(
    Guid Id,
    Guid ConversationId,
    Guid SenderOrganizationId,
    string? SenderOrganizationName,
    Guid SenderUserId,
    string? SenderUserDisplayName,
    string? Text,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime? EditedAt,
    DateTime? DeletedAt,
    Guid? DeletedByUserId);

public sealed record ChatContactRequestResponse(
    Guid Id,
    Guid RequesterOrganizationId,
    string? RequesterOrganizationName,
    Guid TargetOrganizationId,
    string? TargetOrganizationName,
    Guid RequesterUserId,
    string? RequesterUserDisplayName,
    string? Message,
    ChatContactRequestStatus Status,
    DateTime CreatedAt,
    DateTime? RespondedAt,
    Guid? RespondedByUserId,
    Guid? ConversationId,
    string? RejectionReason,
    DateTime? CancelledAt,
    Guid? CancelledByUserId);

public sealed record ChatConversationReadResponse(
    Guid ConversationId,
    Guid OrganizationId,
    Guid UserId,
    Guid? LastReadMessageId,
    DateTime LastReadAt);

public sealed record ChatTypingResponse(
    Guid ConversationId,
    Guid OrganizationId,
    Guid UserId,
    string? UserDisplayName);
