using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Contracts;

public sealed record StockMovementResponse(
    Guid Id,
    Guid OrganizationId,
    Guid StorageId,
    string StorageName,
    Guid ProductId,
    Guid? DealId,
    Guid? SourceReturnId,
    StockMovementType Type,
    decimal Quantity,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string? Reason,
    DateTime CreatedAt,
    Guid? CreatedByUserId);

internal static class StockMovementResponseMapper
{
    public static StockMovementResponse ToResponse(this StockMovement movement, string storageName)
    {
        return new StockMovementResponse(
            movement.Id,
            movement.OrganizationId,
            movement.StorageId,
            storageName,
            movement.ProductId,
            movement.DealId,
            movement.SourceReturnId,
            movement.Type,
            movement.Quantity,
            movement.QuantityBefore,
            movement.QuantityAfter,
            movement.Reason,
            movement.CreatedAt,
            movement.CreatedByUserId);
    }
}
