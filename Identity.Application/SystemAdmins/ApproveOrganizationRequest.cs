using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Email;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.SystemAdmins;

public sealed record ApproveOrganizationRequestCommand(Guid RequestId) : IRequest<ActivationKeyResponse>;

public sealed record ActivationKeyResponse(string ActivationKey);

public sealed class ApproveOrganizationRequestCommandValidator : AbstractValidator<ApproveOrganizationRequestCommand>
{
    public ApproveOrganizationRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
    }
}

public sealed class ApproveOrganizationRequestCommandHandler
    : IRequestHandler<ApproveOrganizationRequestCommand, ActivationKeyResponse>
{
    private readonly IOrganizationRequestRepository _organizationRequestRepository;
    private readonly IActivationKeyRepository _activationKeyRepository;
    private readonly IActivationKeyGenerator _activationKeyGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveOrganizationRequestCommandHandler(
        IOrganizationRequestRepository organizationRequestRepository,
        IActivationKeyRepository activationKeyRepository,
        IActivationKeyGenerator activationKeyGenerator,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _organizationRequestRepository = organizationRequestRepository;
        _activationKeyRepository = activationKeyRepository;
        _activationKeyGenerator = activationKeyGenerator;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ActivationKeyResponse> Handle(ApproveOrganizationRequestCommand request, CancellationToken cancellationToken)
    {
        var systemAdminId = HandlerGuards.RequireSystemAdminId(_currentUserService);
        var organizationRequest = await _organizationRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException("Organization request was not found");

        if (organizationRequest.Status != OrganizationRequestStatus.Pending)
        {
            throw new ConflictException("Organization request is already processed");
        }

        var now = _dateTimeProvider.UtcNow;
        var activationKeyPlain = _activationKeyGenerator.Generate();
        var activationKey = new ActivationKey(
            Guid.NewGuid(),
            _tokenHasher.Hash(activationKeyPlain),
            organizationRequest.Id,
            expiresAt: null,
            now,
            systemAdminId);

        organizationRequest.Approve(systemAdminId, now);

        await _activationKeyRepository.AddAsync(activationKey, cancellationToken);
        await _emailSender.SendAsync(
            organizationRequest.ContactEmail,
            "CRM activation key",
            $"Your CRM activation key: {activationKeyPlain}",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivationKeyResponse(activationKeyPlain);
    }
}
