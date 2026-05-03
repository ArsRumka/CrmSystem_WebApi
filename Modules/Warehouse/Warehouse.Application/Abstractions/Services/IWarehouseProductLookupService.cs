namespace Warehouse.Application.Abstractions.Services;

public interface IWarehouseProductLookupService
{
    Task<bool> ExistsAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken);

    Task<bool> ExistsActiveAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken);
}

