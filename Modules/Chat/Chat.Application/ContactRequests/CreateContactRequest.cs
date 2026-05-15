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
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.ContactRequests;

public sealed record CreateContactRequestCommand(string TargetOrganizationEmail, string? Message)
    : IRequest<ChatContactRequestResponse>;

public sealed class CreateContactRequestCommandValidator : AbstractValidator<CreateContactRequestCommand>
{
    public CreateContactRequestCommandValidator()
    {
        RuleFor(x => x.TargetOrganizationEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Message).MaximumLength(1000);
    }
}

public sealed class CreateContactRequestCommandHandler
    : IRequestHandler<CreateContactRequestCommand, ChatContactRequestResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatOrganizationLookupService _organizationLookupService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatContactRequestRepository _contactRequestRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public CreateContactRequestCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatOrganizationLookupService organizationLookupService,
        IChatConversationRepository conversationRepository,
        IChatContactRequestRepository contactRequestRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _organizationLookupService = organizationLookupService;
        _conversationRepository = conversationRepository;
        _contactRequestRepository = contactRequestRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatContactRequestResponse> Handle(
        CreateContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Create, cancellationToken);

        var targetOrganizationId = await _organizationLookupService.GetOrganizationIdByEmailAsync(
            request.TargetOrganizationEmail,
            cancellationToken)
            ?? throw new NotFoundException("Target organization was not found");

        if (targetOrganizationId == organizationId)
        {
            throw new ConflictException("Cannot create contact request to own organization");
        }

        if (await _contactRequestRepository.PendingExistsAsync(organizationId, targetOrganizationId, cancellationToken))
        {
            throw new ConflictException("Pending contact request already exists");
        }

        if (await _conversationRepository.ActiveInterOrganizationConversationExistsAsync(
                organizationId,
                targetOrganizationId,
                cancellationToken))
        {
            throw new ConflictException("Active inter-organization conversation already exists");
        }

        var contactRequest = new ChatContactRequest(
            Guid.NewGuid(),
            organizationId,
            targetOrganizationId,
            userId,
            request.Message,
            _dateTimeProvider.UtcNow);

        await _contactRequestRepository.AddAsync(contactRequest, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Chat",
            AuditAction.Create,
            "ChatContactRequest",
            contactRequest.Id,
            $"Chat contact request {contactRequest.Id} was created",
            oldValues: null,
            newValues: ChatAuditSnapshots.ContactRequest(contactRequest),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateContactRequestResponseAsync(contactRequest, cancellationToken);
    }
}
