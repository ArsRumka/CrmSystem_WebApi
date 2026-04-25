using BuildingBlocks.Application.Abstractions.Auth;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.SystemAdmins;

public sealed record GetOrganizationRequestsQuery(OrganizationRequestStatus? Status) : IRequest<IReadOnlyList<OrganizationRequestResponse>>;

public sealed class GetOrganizationRequestsQueryHandler
    : IRequestHandler<GetOrganizationRequestsQuery, IReadOnlyList<OrganizationRequestResponse>>
{
    private readonly IOrganizationRequestRepository _organizationRequestRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetOrganizationRequestsQueryHandler(
        IOrganizationRequestRepository organizationRequestRepository,
        ICurrentUserService currentUserService)
    {
        _organizationRequestRepository = organizationRequestRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<OrganizationRequestResponse>> Handle(
        GetOrganizationRequestsQuery request,
        CancellationToken cancellationToken)
    {
        HandlerGuards.RequireSystemAdminId(_currentUserService);

        var requests = await _organizationRequestRepository.GetAllAsync(cancellationToken);

        if (request.Status.HasValue)
        {
            requests = requests.Where(x => x.Status == request.Status.Value).ToList();
        }

        return requests.Select(Map).ToList();
    }

    private static OrganizationRequestResponse Map(OrganizationRequest request)
    {
        return new OrganizationRequestResponse(
            request.Id,
            request.CompanyName,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone,
            request.Comment,
            request.Status,
            request.CreatedAt,
            request.ProcessedAt,
            request.ProcessedBySystemAdminId);
    }
}
