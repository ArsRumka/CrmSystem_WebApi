using Deals.Domain.Entities;

namespace Deals.Application.Abstractions.Repositories;

public interface IDealStageHistoryRepository
{
    Task AddAsync(DealStageHistory history, CancellationToken cancellationToken);

    Task<List<DealStageHistory>> GetByDealIdAsync(
        Guid organizationId,
        Guid dealId,
        CancellationToken cancellationToken);
}
