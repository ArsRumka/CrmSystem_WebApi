using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.OrganizationRequests;

public sealed record CreateOrganizationRequestCommand(
    string CompanyName,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string? Comment) : IRequest<RequestIdResponse>;

public sealed class CreateOrganizationRequestCommandValidator : AbstractValidator<CreateOrganizationRequestCommand>
{
    public CreateOrganizationRequestCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty();
        RuleFor(x => x.ContactName).NotEmpty();
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.ContactPhone).NotEmpty();
    }
}

public sealed class CreateOrganizationRequestCommandHandler
    : IRequestHandler<CreateOrganizationRequestCommand, RequestIdResponse>
{
    private readonly IOrganizationRequestRepository _organizationRequestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateOrganizationRequestCommandHandler(
        IOrganizationRequestRepository organizationRequestRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _organizationRequestRepository = organizationRequestRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RequestIdResponse> Handle(CreateOrganizationRequestCommand request, CancellationToken cancellationToken)
    {
        var organizationRequest = new OrganizationRequest(
            Guid.NewGuid(),
            request.CompanyName,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.Comment,
            _dateTimeProvider.UtcNow);

        await _organizationRequestRepository.AddAsync(organizationRequest, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestIdResponse(organizationRequest.Id);
    }
}
