using Catalog.Domain.Enums;

namespace Catalog.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid? ParentCategoryId { get; private set; }
    public BonusType BonusType { get; private set; }
    public decimal? BonusValue { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal? DiscountValue { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Category()
    {
    }

    public Category(
        Guid id,
        Guid organizationId,
        string name,
        Guid? parentCategoryId,
        BonusType bonusType,
        decimal? bonusValue,
        DiscountType discountType,
        decimal? discountValue,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        ValidateBonusRule(bonusType, bonusValue);
        ValidateDiscountRule(discountType, discountValue);

        Id = id;
        OrganizationId = organizationId;
        Name = Require(name, nameof(name));
        ParentCategoryId = parentCategoryId;
        BonusType = bonusType;
        BonusValue = bonusValue;
        DiscountType = discountType;
        DiscountValue = discountValue;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public void Update(
        string name,
        Guid? parentCategoryId,
        BonusType bonusType,
        decimal? bonusValue,
        DiscountType discountType,
        decimal? discountValue,
        DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        ValidateBonusRule(bonusType, bonusValue);
        ValidateDiscountRule(discountType, discountValue);

        Name = Require(name, nameof(name));
        ParentCategoryId = parentCategoryId;
        BonusType = bonusType;
        BonusValue = bonusValue;
        DiscountType = discountType;
        DiscountValue = discountValue;
        UpdatedAt = updatedAt;
    }

    public void Deactivate(DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        IsActive = false;
        UpdatedAt = updatedAt;
    }

    private static string Require(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required", parameterName);

        return value.Trim();
    }

    private static void ValidateBonusRule(BonusType type, decimal? value)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid bonus type", nameof(type));

        if (!IsValidRule(type, value))
            throw new ArgumentException("Invalid bonus value", nameof(value));
    }

    private static void ValidateDiscountRule(DiscountType type, decimal? value)
    {
        if (!Enum.IsDefined(type))
            throw new ArgumentException("Invalid discount type", nameof(type));

        if (!IsValidRule(type, value))
            throw new ArgumentException("Invalid discount value", nameof(value));
    }

    private static bool IsValidRule(BonusType type, decimal? value)
    {
        return type switch
        {
            BonusType.Percent => value.HasValue && value.Value > 0 && value.Value <= 100,
            BonusType.Fixed => value.HasValue && value.Value > 0,
            BonusType.None or BonusType.Inherit => !value.HasValue || value.Value == 0,
            _ => false
        };
    }

    private static bool IsValidRule(DiscountType type, decimal? value)
    {
        return type switch
        {
            DiscountType.Percent => value.HasValue && value.Value > 0 && value.Value <= 100,
            DiscountType.Fixed => value.HasValue && value.Value > 0,
            DiscountType.None or DiscountType.Inherit => !value.HasValue || value.Value == 0,
            _ => false
        };
    }
}
