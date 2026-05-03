using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts;

public sealed record ProductStockResponse(
    Guid Id,
    Guid OrganizationId,
    Guid StorageId,
    string StorageName,
    Guid ProductId,
    decimal Quantity,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class ProductStockResponseMapper
{
    public static ProductStockResponse ToResponse(this ProductStock stock, string storageName)
    {
        return new ProductStockResponse(
            stock.Id,
            stock.OrganizationId,
            stock.StorageId,
            storageName,
            stock.ProductId,
            stock.Quantity,
            stock.CreatedAt,
            stock.UpdatedAt);
    }
}

