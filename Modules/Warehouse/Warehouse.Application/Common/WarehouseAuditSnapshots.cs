using Warehouse.Domain.Entities;

namespace Warehouse.Application.Common;

internal static class WarehouseAuditSnapshots
{
    public static object Storage(Storage storage)
    {
        return new
        {
            storage.Name,
            storage.Address,
            storage.IsDefault,
            storage.IsActive
        };
    }

    public static object StockMovement(
        Guid storageId,
        Guid productId,
        decimal quantity,
        decimal quantityBefore,
        decimal quantityAfter,
        string movementType)
    {
        return new
        {
            StorageId = storageId,
            ProductId = productId,
            MovementType = movementType,
            Quantity = quantity,
            QuantityBefore = quantityBefore,
            QuantityAfter = quantityAfter
        };
    }
}
