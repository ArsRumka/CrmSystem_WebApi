using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using FluentValidation;
using MediatR;

namespace Chat.Application.Realtime;

public sealed record JoinConversationCommand(
    Guid ConversationId,
    Guid? ActorOrganizationId = null,
    Guid? ActorUserId = null) : IRequest;

public sealed class JoinConversationCommandValidator : AbstractValidator<JoinConversationCommand>
{
    public JoinConversationCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ActorOrganizationId).NotEmpty().When(x => x.ActorOrganizationId.HasValue);
        RuleFor(x => x.ActorUserId).NotEmpty().When(x => x.ActorUserId.HasValue);
    }
}

public sealed class JoinConversationCommandHandler : IRequestHandler<JoinConversationCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;

    public JoinConversationCommandHandler(
        ICurrentUserService currentUserService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository)
    {
        _currentUserService = currentUserService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
    }

    public async Task Handle(JoinConversationCommand request, CancellationToken cancellationToken)
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
    }
}
