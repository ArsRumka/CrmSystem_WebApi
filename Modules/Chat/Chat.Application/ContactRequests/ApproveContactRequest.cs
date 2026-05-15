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

namespace Chat.Application.ContactRequests;

public sealed record ApproveContactRequestCommand(Guid Id) : IRequest<ChatConversationResponse>;

public sealed class ApproveContactRequestCommandValidator : AbstractValidator<ApproveContactRequestCommand>
{
    public ApproveContactRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class ApproveContactRequestCommandHandler
    : IRequestHandler<ApproveContactRequestCommand, ChatConversationResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatContactRequestRepository _contactRequestRepository;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatUserLookupService _userLookupService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public ApproveContactRequestCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatContactRequestRepository contactRequestRepository,
        IChatConversationRepository conversationRepository,
        IChatUserLookupService userLookupService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _contactRequestRepository = contactRequestRepository;
        _conversationRepository = conversationRepository;
        _userLookupService = userLookupService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatConversationResponse> Handle(
        ApproveContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Contact request was not found");

        if (contactRequest.TargetOrganizationId != organizationId)
        {
            throw new ForbiddenException("Only target organization can approve this request");
        }

        if (contactRequest.Status != ChatContactRequestStatus.Pending)
        {
            throw new ConflictException("Only pending contact requests can be approved");
        }

        if (!await _userLookupService.ExistsActiveInOrganizationAsync(
                contactRequest.RequesterOrganizationId,
                contactRequest.RequesterUserId,
                cancellationToken))
        {
            throw new ConflictException("Requester user is not active");
        }

        if (await _conversationRepository.ActiveInterOrganizationConversationExistsAsync(
                contactRequest.RequesterOrganizationId,
                contactRequest.TargetOrganizationId,
                cancellationToken))
        {
            throw new ConflictException("Active inter-organization conversation already exists");
        }

        var now = _dateTimeProvider.UtcNow;
        var conversation = new ChatConversation(
            Guid.NewGuid(),
            ChatConversationType.InterOrganization,
            contactRequest.RequesterOrganizationId,
            title: null,
            clientId: null,
            dealId: null,
            userId,
            now);

        conversation.AddOrganization(new ChatConversationOrganization(
            Guid.NewGuid(),
            conversation.Id,
            contactRequest.RequesterOrganizationId,
            now));
        conversation.AddOrganization(new ChatConversationOrganization(
            Guid.NewGuid(),
            conversation.Id,
            contactRequest.TargetOrganizationId,
            now));

        conversation.AddParticipant(new ChatParticipant(
            Guid.NewGuid(),
            conversation.Id,
            contactRequest.RequesterOrganizationId,
            contactRequest.RequesterUserId,
            now));
        conversation.AddParticipant(new ChatParticipant(
            Guid.NewGuid(),
            conversation.Id,
            organizationId,
            userId,
            now));

        var oldSnapshot = ChatAuditSnapshots.ContactRequest(contactRequest);

        contactRequest.Approve(conversation.Id, userId, now);

        await _conversationRepository.AddAsync(conversation, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Chat",
            AuditAction.Approve,
            "ChatContactRequest",
            contactRequest.Id,
            $"Chat contact request {contactRequest.Id} was approved",
            oldSnapshot,
            ChatAuditSnapshots.ContactRequest(contactRequest),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateConversationResponseAsync(conversation, userId, cancellationToken);
    }
}
