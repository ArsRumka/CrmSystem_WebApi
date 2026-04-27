using Deals.Domain.Entities;

namespace Deals.Application.Abstractions.Repositories;

public interface IDealStageRepository
{
    Task AddAsync(DealStage stage, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<DealStage> stages, CancellationToken cancellationToken);

    Task<DealStage?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<DealStage>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<List<DealStage>> SearchAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> AnyAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<DealStage?> GetInitialStageAsync(Guid organizationId, CancellationToken cancellationToken);
}
