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

public sealed record DeleteMessageCommand(Guid MessageId) : IRequest<ChatMessageResponse>;

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).NotEmpty();
    }
}

public sealed class DeleteMessageCommandHandler : IRequestHandler<DeleteMessageCommand, ChatMessageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public DeleteMessageCommandHandler(
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

    public async Task<ChatMessageResponse> Handle(DeleteMessageCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);

        var message = await _messageRepository.GetByIdAsync(request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Message was not found");

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(message.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        if (message.IsDeleted)
        {
            throw new ConflictException("Message is already deleted");
        }

        if (message.SenderUserId != userId)
        {
            var canDelete = await _permissionService.HasPermissionAsync(
                userId,
                ChatApplicationGuards.ModuleCode,
                PermissionAction.Delete,
                cancellationToken);
            var canUpdate = await _permissionService.HasPermissionAsync(
                userId,
                ChatApplicationGuards.ModuleCode,
                PermissionAction.Update,
                cancellationToken);

            if (!canDelete && !canUpdate)
            {
                throw new ForbiddenException("Only the sender or a user with Chat delete/update permission can delete the message");
            }
        }

        message.SoftDelete(userId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateMessageResponseAsync(message, cancellationToken);
    }
}
