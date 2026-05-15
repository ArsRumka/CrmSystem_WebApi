using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Lookups;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Conversations;

public sealed record CreateConversationCommand(
    ChatConversationType Type,
    string? Title,
    IReadOnlyList<Guid> ParticipantUserIds,
    Guid? ClientId,
    Guid? DealId) : IRequest<ChatConversationResponse>;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.ParticipantUserIds).NotNull();
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();
        RuleFor(x => x.ClientId).NotEmpty().When(x => x.ClientId.HasValue);
        RuleFor(x => x.DealId).NotEmpty().When(x => x.DealId.HasValue);
        RuleFor(x => x.ClientId).NotNull().When(x => x.Type == ChatConversationType.Client);
        RuleFor(x => x.DealId).NotNull().When(x => x.Type == ChatConversationType.Deal);
        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("ParticipantUserIds must be unique");
        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids.Count == 1)
            .When(x => x.Type == ChatConversationType.Direct)
            .WithMessage("Direct conversation requires exactly one other participant");
        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids.Count >= 1)
            .When(x => x.Type == ChatConversationType.Group)
            .WithMessage("Group conversation requires at least one other participant");
    }
}

public sealed class CreateConversationCommandHandler
    : IRequestHandler<CreateConversationCommand, ChatConversationResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatUserLookupService _userLookupService;
    private readonly IChatClientLookupService _clientLookupService;
    private readonly IChatDealLookupService _dealLookupService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public CreateConversationCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatUserLookupService userLookupService,
        IChatClientLookupService clientLookupService,
        IChatDealLookupService dealLookupService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _userLookupService = userLookupService;
        _clientLookupService = clientLookupService;
        _dealLookupService = dealLookupService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatConversationResponse> Handle(
        CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(
            _permissionService,
            userId,
            PermissionAction.Create,
            cancellationToken);

        if (request.Type == ChatConversationType.InterOrganization)
        {
            throw new ConflictException("Inter-organization conversations can be created only by approving a contact request");
        }

        if (request.ParticipantUserIds.Contains(userId))
        {
            throw new ConflictException("Current user is added automatically and must not be included in ParticipantUserIds");
        }

        if (request.Type == ChatConversationType.Direct)
        {
            var otherUserId = request.ParticipantUserIds.Single();
            await EnsureUserExistsAsync(organizationId, otherUserId, cancellationToken);

            var existing = await _conversationRepository.FindDirectConversationAsync(
                organizationId,
                userId,
                otherUserId,
                cancellationToken);

            if (existing is not null)
            {
                return await _responseFactory.CreateConversationResponseAsync(existing, userId, cancellationToken);
            }
        }

        if (request.Type == ChatConversationType.Client &&
            !await _clientLookupService.ExistsAsync(organizationId, request.ClientId!.Value, cancellationToken))
        {
            throw new NotFoundException("Client was not found");
        }

        if (request.Type == ChatConversationType.Deal &&
            !await _dealLookupService.ExistsAsync(organizationId, request.DealId!.Value, cancellationToken))
        {
            throw new NotFoundException("Deal was not found");
        }

        foreach (var participantUserId in request.ParticipantUserIds)
        {
            await EnsureUserExistsAsync(organizationId, participantUserId, cancellationToken);
        }

        var now = _dateTimeProvider.UtcNow;
        var conversation = new ChatConversation(
            Guid.NewGuid(),
            request.Type,
            organizationId,
            request.Title,
            request.ClientId,
            request.DealId,
            userId,
            now);

        conversation.AddOrganization(new ChatConversationOrganization(
            Guid.NewGuid(),
            conversation.Id,
            organizationId,
            now));

        conversation.AddParticipant(new ChatParticipant(
            Guid.NewGuid(),
            conversation.Id,
            organizationId,
            userId,
            now));

        foreach (var participantUserId in request.ParticipantUserIds)
        {
            conversation.AddParticipant(new ChatParticipant(
                Guid.NewGuid(),
                conversation.Id,
                organizationId,
                participantUserId,
                now));
        }

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Chat",
            AuditAction.Create,
            "ChatConversation",
            conversation.Id,
            $"Chat conversation {conversation.Id} was created",
            oldValues: null,
            newValues: ChatAuditSnapshots.Conversation(conversation),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateConversationResponseAsync(conversation, userId, cancellationToken);
    }

    private async Task EnsureUserExistsAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await _userLookupService.ExistsActiveInOrganizationAsync(organizationId, userId, cancellationToken))
        {
            throw new NotFoundException("User was not found");
        }
    }
}
