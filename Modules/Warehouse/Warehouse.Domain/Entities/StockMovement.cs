using Warehouse.Domain.Enums;

namespace Warehouse.Domain.Entities;

public class StockMovement
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid StorageId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? DealId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal QuantityBefore { get; private set; }
    public decimal QuantityAfter { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private StockMovement()
    {
    }

    public StockMovement(
        Guid id,
        Guid organizationId,
        Guid storageId,
        Guid productId,
        Guid? dealId,
        StockMovementType type,
        decimal quantity,
        decimal quantityBefore,
        decimal quantityAfter,
        string? reason,
        DateTime createdAt,
        Guid? createdByUserId)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (storageId == Guid.Empty)
            throw new ArgumentException("StorageId is required", nameof(storageId));

        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId is required", nameof(productId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId cannot be empty", nameof(dealId));

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("CreatedByUserId cannot be empty", nameof(createdByUserId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid stock movement type", nameof(type));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (quantityBefore < 0)
            throw new ArgumentException("QuantityBefore must be greater than or equal to zero", nameof(quantityBefore));

        if (quantityAfter < 0)
            throw new ArgumentException("QuantityAfter must be greater than or equal to zero", nameof(quantityAfter));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Id = id;
        OrganizationId = organizationId;
        StorageId = storageId;
        ProductId = productId;
        DealId = dealId;
        Type = type;
        Quantity = quantity;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        Reason = NormalizeOptional(reason, nameof(reason), 1000);
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
    }

    private static string? NormalizeOptional(string? value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }
}

