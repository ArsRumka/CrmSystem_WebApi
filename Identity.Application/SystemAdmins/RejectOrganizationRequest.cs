using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.SystemAdmins;

public sealed record RejectOrganizationRequestCommand(Guid RequestId, string? Reason) : IRequest<SuccessResponse>;

public sealed class RejectOrganizationRequestCommandValidator : AbstractValidator<RejectOrganizationRequestCommand>
{
    public RejectOrganizationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class RejectOrganizationRequestCommandHandler : IRequestHandler<RejectOrganizationRequestCommand, SuccessResponse>
{
    private readonly IOrganizationRequestRepository _organizationRequestRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RejectOrganizationRequestCommandHandler(
        IOrganizationRequestRepository organizationRequestRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _organizationRequestRepository = organizationRequestRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(RejectOrganizationRequestCommand request, CancellationToken cancellationToken)
    {
        var systemAdminId = HandlerGuards.RequireSystemAdminId(_currentUserService);
        var organizationRequest = await _organizationRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("Organization request was not found");

        if (organizationRequest.Status != OrganizationRequestStatus.Pending)
        {
            throw new ConflictException("Organization request is already processed");
        }

        organizationRequest.Reject(systemAdminId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
