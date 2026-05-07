using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Messages;

public sealed record MarkConversationReadCommand(
    Guid ConversationId,
    Guid? MessageId,
    Guid? ActorOrganizationId = null,
    Guid? ActorUserId = null) : IRequest<ChatConversationReadResponse>;

public sealed class MarkConversationReadCommandValidator : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.MessageId).NotEmpty().When(x => x.MessageId.HasValue);
        RuleFor(x => x.ActorOrganizationId).NotEmpty().When(x => x.ActorOrganizationId.HasValue);
        RuleFor(x => x.ActorUserId).NotEmpty().When(x => x.ActorUserId.HasValue);
    }
}

public sealed class MarkConversationReadCommandHandler
    : IRequestHandler<MarkConversationReadCommand, ChatConversationReadResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public MarkConversationReadCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IChatMessageRepository messageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ChatConversationReadResponse> Handle(
        MarkConversationReadCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(
            _currentUserService,
            request.ActorOrganizationId,
            request.ActorUserId);

        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        var participant = await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        if (request.MessageId.HasValue)
        {
            var message = await _messageRepository.GetByIdAsync(request.MessageId.Value, cancellationToken)
                ?? throw new NotFoundException("Message was not found");

            if (message.ConversationId != conversation.Id)
            {
                throw new ConflictException("Message does not belong to this conversation");
            }
        }

        var now = _dateTimeProvider.UtcNow;
        participant.MarkRead(request.MessageId, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ChatConversationReadResponse(conversation.Id, organizationId, userId, request.MessageId, now);
    }
}
