namespace Warehouse.Application.Abstractions.Services;

public sealed record WarehouseDealReturnItem(
    Guid DealItemId,
    Guid ProductId,
    Guid StorageId,
    decimal Quantity);

public interface IWarehouseDealReturnService
{
    Task ProcessReturnAsync(
        Guid organizationId,
        Guid dealId,
        Guid dealReturnId,
        Guid userId,
        IReadOnlyCollection<WarehouseDealReturnItem> items,
        string reason,
        CancellationToken cancellationToken);
}
