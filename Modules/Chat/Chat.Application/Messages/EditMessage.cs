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

public sealed record EditMessageCommand(Guid MessageId, string Text) : IRequest<ChatMessageResponse>;

public sealed class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(4000);
    }
}

public sealed class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, ChatMessageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public EditMessageCommandHandler(
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

    public async Task<ChatMessageResponse> Handle(EditMessageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Message was not found");

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(message.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        if (message.SenderUserId != userId)
        {
            throw new ForbiddenException("Only the sender can edit the message");
        }

        if (message.IsDeleted)
        {
            throw new ConflictException("Deleted messages cannot be edited");
        }

        message.Edit(request.Text, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateMessageResponseAsync(message, cancellationToken);
    }
}
