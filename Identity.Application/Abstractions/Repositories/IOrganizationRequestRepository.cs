using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IOrganizationRequestRepository
{
    Task AddAsync(OrganizationRequest organizationRequest, CancellationToken cancellationToken = default);

    Task<OrganizationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationRequest>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationRequest>> GetAllAsync(CancellationToken cancellationToken = default);
}
