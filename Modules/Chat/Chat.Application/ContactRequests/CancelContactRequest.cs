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

namespace Chat.Application.ContactRequests;

public sealed record CancelContactRequestCommand(Guid Id) : IRequest<ChatContactRequestResponse>;

public sealed class CancelContactRequestCommandValidator : AbstractValidator<CancelContactRequestCommand>
{
    public CancelContactRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CancelContactRequestCommandHandler
    : IRequestHandler<CancelContactRequestCommand, ChatContactRequestResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatContactRequestRepository _contactRequestRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public CancelContactRequestCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatContactRequestRepository contactRequestRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _contactRequestRepository = contactRequestRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatContactRequestResponse> Handle(
        CancelContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Contact request was not found");

        if (contactRequest.RequesterOrganizationId != organizationId)
        {
            throw new ForbiddenException("Only requester organization can cancel this request");
        }

        if (contactRequest.Status != ChatContactRequestStatus.Pending)
        {
            throw new ConflictException("Only pending contact requests can be cancelled");
        }

        var oldSnapshot = ChatAuditSnapshots.ContactRequest(contactRequest);

        contactRequest.Cancel(userId, _dateTimeProvider.UtcNow);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Chat",
            AuditAction.Cancel,
            "ChatContactRequest",
            contactRequest.Id,
            $"Chat contact request {contactRequest.Id} was cancelled",
            oldSnapshot,
            ChatAuditSnapshots.ContactRequest(contactRequest),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateContactRequestResponseAsync(contactRequest, cancellationToken);
    }
}
