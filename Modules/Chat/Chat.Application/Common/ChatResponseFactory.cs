using Chat.Application.Abstractions.Lookups;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Contracts;
using Chat.Domain.Entities;

namespace Chat.Application.Common;

public sealed class ChatResponseFactory
{
    private readonly IChatMessageRepository _messageRepository;
    private readonly IChatOrganizationLookupService _organizationLookupService;
    private readonly IChatUserLookupService _userLookupService;

    public ChatResponseFactory(
        IChatMessageRepository messageRepository,
        IChatOrganizationLookupService organizationLookupService,
        IChatUserLookupService userLookupService)
    {
        _messageRepository = messageRepository;
        _organizationLookupService = organizationLookupService;
        _userLookupService = userLookupService;
    }

    public async Task<ChatConversationResponse> CreateConversationResponseAsync(
        ChatConversation conversation,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var lastMessage = await _messageRepository.GetLastMessageAsync(conversation.Id, cancellationToken);
        var currentParticipant = conversation.Participants.FirstOrDefault(x => x.UserId == currentUserId);
        var unreadCount = await _messageRepository.CountUnreadAsync(
            conversation.Id,
            currentUserId,
            currentParticipant?.LastReadAt,
            cancellationToken);

        return new ChatConversationResponse(
            conversation.Id,
            conversation.Type,
            conversation.OwnerOrganizationId,
            conversation.Title,
            conversation.ClientId,
            conversation.DealId,
            conversation.IsActive,
            conversation.CreatedAt,
            conversation.CreatedByUserId,
            conversation.UpdatedAt,
            conversation.DeletedAt,
            await CreateOrganizationResponsesAsync(conversation.Organizations, cancellationToken),
            await CreateParticipantResponsesAsync(conversation.Participants, cancellationToken),
            lastMessage is null ? null : await CreateMessageResponseAsync(lastMessage, cancellationToken),
            lastMessage?.CreatedAt,
            unreadCount);
    }

    public async Task<IReadOnlyList<ChatConversationResponse>> CreateConversationResponsesAsync(
        IEnumerable<ChatConversation> conversations,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        var responses = new List<ChatConversationResponse>();
        foreach (var conversation in conversations)
        {
            responses.Add(await CreateConversationResponseAsync(conversation, currentUserId, cancellationToken));
        }

        return responses;
    }

    public async Task<ChatParticipantResponse> CreateParticipantResponseAsync(
        ChatParticipant participant,
        CancellationToken cancellationToken)
    {
        var organizationName = await _organizationLookupService.GetOrganizationNameAsync(
            participant.OrganizationId,
            cancellationToken);
        var userDisplayName = await _userLookupService.GetUserDisplayNameAsync(
            participant.OrganizationId,
            participant.UserId,
            cancellationToken);

        return new ChatParticipantResponse(
            participant.Id,
            participant.ConversationId,
            participant.OrganizationId,
            organizationName,
            participant.UserId,
            userDisplayName,
            participant.IsActive,
            participant.JoinedAt,
            participant.LeftAt,
            participant.LastReadMessageId,
            participant.LastReadAt);
    }

    public async Task<ChatMessageResponse> CreateMessageResponseAsync(
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        var organizationName = await _organizationLookupService.GetOrganizationNameAsync(
            message.SenderOrganizationId,
            cancellationToken);
        var senderDisplayName = await _userLookupService.GetUserDisplayNameAsync(
            message.SenderOrganizationId,
            message.SenderUserId,
            cancellationToken);

        return new ChatMessageResponse(
            message.Id,
            message.ConversationId,
            message.SenderOrganizationId,
            organizationName,
            message.SenderUserId,
            senderDisplayName,
            message.IsDeleted ? null : message.Text,
            message.IsDeleted,
            message.CreatedAt,
            message.EditedAt,
            message.DeletedAt,
            message.DeletedByUserId);
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> CreateMessageResponsesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var responses = new List<ChatMessageResponse>();
        foreach (var message in messages)
        {
            responses.Add(await CreateMessageResponseAsync(message, cancellationToken));
        }

        return responses;
    }

    public async Task<ChatContactRequestResponse> CreateContactRequestResponseAsync(
        ChatContactRequest request,
        CancellationToken cancellationToken)
    {
        var requesterOrganizationName = await _organizationLookupService.GetOrganizationNameAsync(
            request.RequesterOrganizationId,
            cancellationToken);
        var targetOrganizationName = await _organizationLookupService.GetOrganizationNameAsync(
            request.TargetOrganizationId,
            cancellationToken);
        var requesterUserDisplayName = await _userLookupService.GetUserDisplayNameAsync(
            request.RequesterOrganizationId,
            request.RequesterUserId,
            cancellationToken);

        return new ChatContactRequestResponse(
            request.Id,
            request.RequesterOrganizationId,
            requesterOrganizationName,
            request.TargetOrganizationId,
            targetOrganizationName,
            request.RequesterUserId,
            requesterUserDisplayName,
            request.Message,
            request.Status,
            request.CreatedAt,
            request.RespondedAt,
            request.RespondedByUserId,
            request.ConversationId,
            request.RejectionReason,
            request.CancelledAt,
            request.CancelledByUserId);
    }

    public async Task<IReadOnlyList<ChatContactRequestResponse>> CreateContactRequestResponsesAsync(
        IEnumerable<ChatContactRequest> requests,
        CancellationToken cancellationToken)
    {
        var responses = new List<ChatContactRequestResponse>();
        foreach (var request in requests)
        {
            responses.Add(await CreateContactRequestResponseAsync(request, cancellationToken));
        }

        return responses;
    }

    private async Task<IReadOnlyList<ChatConversationOrganizationResponse>> CreateOrganizationResponsesAsync(
        IEnumerable<ChatConversationOrganization> organizations,
        CancellationToken cancellationToken)
    {
        var responses = new List<ChatConversationOrganizationResponse>();
        foreach (var organization in organizations.OrderBy(x => x.JoinedAt))
        {
            responses.Add(new ChatConversationOrganizationResponse(
                organization.OrganizationId,
                await _organizationLookupService.GetOrganizationNameAsync(organization.OrganizationId, cancellationToken),
                organization.IsActive,
                organization.JoinedAt,
                organization.LeftAt));
        }

        return responses;
    }

    private async Task<IReadOnlyList<ChatParticipantResponse>> CreateParticipantResponsesAsync(
        IEnumerable<ChatParticipant> participants,
        CancellationToken cancellationToken)
    {
        var responses = new List<ChatParticipantResponse>();
        foreach (var participant in participants.OrderBy(x => x.JoinedAt))
        {
            responses.Add(await CreateParticipantResponseAsync(participant, cancellationToken));
        }

        return responses;
    }
}
