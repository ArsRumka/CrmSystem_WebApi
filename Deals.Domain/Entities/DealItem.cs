using Deals.Domain.Enums;

namespace Deals.Domain.Entities;

public class DealItem
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid DealId { get; private set; }
    public DealItemType ItemType { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid? StorageId { get; private set; }
    public string NameSnapshot { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public decimal PriceAtMoment { get; private set; }
    public DealDiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal FinalAmount { get; private set; }

    private DealItem()
    {
    }

    public DealItem(
        Guid id,
        Guid organizationId,
        Guid dealId,
        DealItemType itemType,
        Guid itemId,
        Guid? storageId,
        string nameSnapshot,
        decimal quantity,
        decimal priceAtMoment,
        DealDiscountType discountType,
        decimal? discountValue,
        decimal discountAmount,
        decimal totalAmount,
        decimal finalAmount)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (dealId == Guid.Empty)
            throw new ArgumentException("DealId is required", nameof(dealId));

        if (itemId == Guid.Empty)
            throw new ArgumentException("ItemId is required", nameof(itemId));

        ValidateItemType(itemType);
        ValidateStorage(itemType, storageId);
        ValidateDiscountRule(discountType, discountValue);
        ValidateAmounts(quantity, priceAtMoment, discountAmount, totalAmount, finalAmount);

        Id = id;
        OrganizationId = organizationId;
        DealId = dealId;
        ItemType = itemType;
        ItemId = itemId;
        StorageId = storageId;
        NameSnapshot = Require(nameSnapshot, nameof(nameSnapshot));
        Quantity = quantity;
        PriceAtMoment = priceAtMoment;
        DiscountType = discountType;
        DiscountValue = NormalizeDiscountValue(discountType, discountValue);
        DiscountAmount = discountAmount;
        TotalAmount = totalAmount;
        FinalAmount = finalAmount;
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        return value.Trim();
    }

    private static decimal? NormalizeDiscountValue(DealDiscountType discountType, decimal? discountValue)
    {
        return discountType == DealDiscountType.None ? null : discountValue;
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
            throw new ArgumentException("StorageId is required for product items", nameof(storageId));

        if (itemType == DealItemType.Service && storageId is not null)
            throw new ArgumentException("StorageId must be null for service items", nameof(storageId));
    }

    private static void ValidateDiscountRule(DealDiscountType type, decimal? value)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid discount type", nameof(type));

        var isValid = type switch
        {
            DealDiscountType.None => !value.HasValue || value.Value == 0,
            DealDiscountType.Percent => value.HasValue && value.Value > 0 && value.Value <= 100,
            DealDiscountType.Fixed => value.HasValue && value.Value > 0,
            _ => false
        };

        if (!isValid)
            throw new ArgumentException("Invalid discount value", nameof(value));
    }

    private static void ValidateAmounts(
        decimal quantity,
        decimal priceAtMoment,
        decimal discountAmount,
        decimal totalAmount,
        decimal finalAmount)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (priceAtMoment < 0)
            throw new ArgumentException("PriceAtMoment must be greater than or equal to zero", nameof(priceAtMoment));

        if (discountAmount < 0)
            throw new ArgumentException("DiscountAmount must be greater than or equal to zero", nameof(discountAmount));

        if (totalAmount < 0)
            throw new ArgumentException("TotalAmount must be greater than or equal to zero", nameof(totalAmount));

        if (finalAmount < 0)
            throw new ArgumentException("FinalAmount must be greater than or equal to zero", nameof(finalAmount));

        if (discountAmount > totalAmount)
            throw new ArgumentException("DiscountAmount cannot exceed TotalAmount", nameof(discountAmount));

        if (finalAmount != totalAmount - discountAmount)
            throw new ArgumentException("FinalAmount must equal TotalAmount minus DiscountAmount", nameof(finalAmount));
    }
}
