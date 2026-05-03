using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Abstractions.Repositories;

public interface IStockMovementRepository
{
    Task AddAsync(StockMovement movement, CancellationToken cancellationToken);

    Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken);

    Task<StockMovement?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsSaleByDealIdAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);

    Task<List<StockMovement>> SearchAsync(
        Guid organizationId,
        Guid? storageId,
        Guid? productId,
        Guid? dealId,
        StockMovementType? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken);
}

