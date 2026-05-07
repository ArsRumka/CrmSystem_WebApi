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

public sealed record RejectContactRequestCommand(Guid Id, string? Reason) : IRequest<ChatContactRequestResponse>;

public sealed class RejectContactRequestCommandValidator : AbstractValidator<RejectContactRequestCommand>
{
    public RejectContactRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class RejectContactRequestCommandHandler
    : IRequestHandler<RejectContactRequestCommand, ChatContactRequestResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatContactRequestRepository _contactRequestRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public RejectContactRequestCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatContactRequestRepository contactRequestRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _contactRequestRepository = contactRequestRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatContactRequestResponse> Handle(
        RejectContactRequestCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var contactRequest = await _contactRequestRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Contact request was not found");

        if (contactRequest.TargetOrganizationId != organizationId)
        {
            throw new ForbiddenException("Only target organization can reject this request");
        }

        if (contactRequest.Status != ChatContactRequestStatus.Pending)
        {
            throw new ConflictException("Only pending contact requests can be rejected");
        }

        contactRequest.Reject(request.Reason, userId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateContactRequestResponseAsync(contactRequest, cancellationToken);
    }
}
