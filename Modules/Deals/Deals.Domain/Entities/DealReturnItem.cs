using Deals.Domain.Enums;

namespace Deals.Domain.Entities;

public class DealReturnItem
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid DealReturnId { get; private set; }
    public Guid DealId { get; private set; }
    public Guid DealItemId { get; private set; }
    public DealItemType ItemType { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid? StorageId { get; private set; }
    public string NameSnapshot { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal ReturnAmount { get; private set; }

    private DealReturnItem()
    {
    }

    public DealReturnItem(
        Guid id,
        Guid organizationId,
        Guid dealReturnId,
        Guid dealId,
        Guid dealItemId,
        DealItemType itemType,
        Guid itemId,
        Guid? storageId,
        string nameSnapshot,
        decimal quantity,
        decimal returnAmount)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (dealReturnId == Guid.Empty)
            throw new ArgumentException("DealReturnId is required", nameof(dealReturnId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required", nameof(dealId));

        if (dealItemId == Guid.Empty)
            throw new ArgumentException("DealItemId is required", nameof(dealItemId));

        if (itemId == Guid.Empty)
            throw new ArgumentException("ItemId is required", nameof(itemId));

        ValidateItemType(itemType);
        ValidateStorage(itemType, storageId);
        ValidateAmounts(quantity, returnAmount);

        Id = id;
        OrganizationId = organizationId;
        DealReturnId = dealReturnId;
        DealId = dealId;
        DealItemId = dealItemId;
        ItemType = itemType;
        ItemId = itemId;
        StorageId = storageId;
        NameSnapshot = Require(nameSnapshot, nameof(nameSnapshot), 300);
        Quantity = quantity;
        ReturnAmount = returnAmount;
    }

    private static string Require(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{parameterName} cannot exceed {maxLength} characters", parameterName);

        return normalized;
    }

    private static void ValidateItemType(DealItemType itemType)
    {
        if (!Enum.IsDefined(itemType))
            throw new ArgumentException("Invalid deal item type", nameof(itemType));
    }

    private static void ValidateStorage(DealItemType itemType, Guid? storageId)
    {
        if (storageId == Guid.Empty)
            throw new ArgumentException("StorageId cannot be empty", nameof(storageId));

        if (itemType == DealItemType.Product && storageId is null)
            throw new ArgumentException("StorageId is required for product return items", nameof(storageId));

        if (itemType == DealItemType.Service && storageId is not null)
            throw new ArgumentException("StorageId must be null for service return items", nameof(storageId));
    }

    private static void ValidateAmounts(decimal quantity, decimal returnAmount)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (returnAmount < 0)
            throw new ArgumentException("ReturnAmount must be greater than or equal to zero", nameof(returnAmount));
    }
}
