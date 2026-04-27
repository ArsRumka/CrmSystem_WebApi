using Deals.Application.Contracts;
using Deals.Domain.Enums;

namespace Deals.Application.Abstractions.Lookups;

public interface ICatalogLookupService
{
    Task<CatalogItemSnapshot?> GetItemSnapshotAsync(
        Guid organizationId,
        DealItemType itemType,
        Guid itemId,
        CancellationToken cancellationToken);
}
