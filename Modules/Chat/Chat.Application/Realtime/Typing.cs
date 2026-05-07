using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Lookups;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Chat.Application.Realtime;

public sealed record TypingCommand(
    Guid ConversationId,
    Guid? ActorOrganizationId = null,
    Guid? ActorUserId = null) : IRequest<ChatTypingResponse>;

public sealed class TypingCommandValidator : AbstractValidator<TypingCommand>
{
    public TypingCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ActorOrganizationId).NotEmpty().When(x => x.ActorOrganizationId.HasValue);
        RuleFor(x => x.ActorUserId).NotEmpty().When(x => x.ActorUserId.HasValue);
    }
}

public sealed class TypingCommandHandler : IRequestHandler<TypingCommand, ChatTypingResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatUserLookupService _userLookupService;

    public TypingCommandHandler(
        ICurrentUserService currentUserService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IChatUserLookupService userLookupService)
    {
        _currentUserService = currentUserService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _userLookupService = userLookupService;
    }

    public async Task<ChatTypingResponse> Handle(TypingCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(
            _currentUserService,
            request.ActorOrganizationId,
            request.ActorUserId);

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        var displayName = await _userLookupService.GetUserDisplayNameAsync(
            organizationId,
            userId,
            cancellationToken);

        return new ChatTypingResponse(conversation.Id, organizationId, userId, displayName);
    }
}
