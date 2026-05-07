using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using Chat.Domain.Entities;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Messages;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string Text,
    Guid? ActorOrganizationId = null,
    Guid? ActorUserId = null) : IRequest<ChatMessageResponse>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ActorOrganizationId).NotEmpty().When(x => x.ActorOrganizationId.HasValue);
        RuleFor(x => x.ActorUserId).NotEmpty().When(x => x.ActorUserId.HasValue);
    }
}

public sealed class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public SendMessageCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IChatMessageRepository messageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatMessageResponse> Handle(
        SendMessageCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(
            _currentUserService,
            request.ActorOrganizationId,
            request.ActorUserId);

        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Create, cancellationToken);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        var message = new ChatMessage(
            Guid.NewGuid(),
            conversation.Id,
            organizationId,
            userId,
            request.Text,
            _dateTimeProvider.UtcNow);

        await _messageRepository.AddAsync(message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateMessageResponseAsync(message, cancellationToken);
    }
}
