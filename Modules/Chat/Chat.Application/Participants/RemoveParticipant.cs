using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using Chat.Domain.Enums;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Participants;

public sealed record RemoveParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<ChatParticipantResponse>;

public sealed class RemoveParticipantCommandValidator : AbstractValidator<RemoveParticipantCommand>
{
    public RemoveParticipantCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class RemoveParticipantCommandHandler
    : IRequestHandler<RemoveParticipantCommand, ChatParticipantResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public RemoveParticipantCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatParticipantResponse> Handle(
        RemoveParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        var targetParticipant = await _participantRepository.GetAsync(
            conversation.Id,
            request.UserId,
            cancellationToken)
            ?? throw new NotFoundException("Participant was not found");

        if (!targetParticipant.IsActive)
        {
            throw new ConflictException("Participant is already inactive");
        }

        if (conversation.Type == ChatConversationType.InterOrganization &&
            targetParticipant.OrganizationId != organizationId)
        {
            throw new ForbiddenException("Users can remove only participants from their organization");
        }

        var activeCount = await _participantRepository.CountActiveParticipantsAsync(conversation.Id, cancellationToken);
        if (activeCount <= 1)
        {
            throw new ConflictException("Cannot remove the last active participant of a conversation");
        }

        if (conversation.Type == ChatConversationType.InterOrganization)
        {
            var activeInOrganization = await _participantRepository.CountActiveParticipantsByOrganizationAsync(
                conversation.Id,
                organizationId,
                cancellationToken);

            if (activeInOrganization <= 1 && targetParticipant.OrganizationId == organizationId)
            {
                throw new ConflictException("Cannot remove the last active participant of current organization");
            }
        }

        var oldSnapshot = ChatAuditSnapshots.Participant(targetParticipant);

        targetParticipant.Deactivate(_dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Chat",
            AuditAction.Delete,
            "ChatParticipant",
            targetParticipant.Id,
            $"Chat participant {targetParticipant.Id} was removed",
            oldSnapshot,
            ChatAuditSnapshots.Participant(targetParticipant),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateParticipantResponseAsync(targetParticipant, cancellationToken);
    }
}
