using Deals.Domain.Entities;

namespace Deals.Application.Abstractions.Repositories;

public interface IDealReturnRepository
{
    Task AddAsync(DealReturn dealReturn, CancellationToken cancellationToken);

    Task<DealReturn?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<DealReturn?> GetByIdWithItemsAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<DealReturn>> GetByDealIdAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);

    Task<List<DealReturn>> GetCompletedByDealIdAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);

    Task<List<DealReturnItem>> GetCompletedItemsByDealIdAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);
}
