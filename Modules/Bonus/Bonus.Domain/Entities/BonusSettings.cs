using Bonus.Domain.Enums;

namespace Bonus.Domain.Entities;

public class BonusSettings
{
    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public bool IsEnabled { get; private set; }
    public decimal PointValue { get; private set; }
    public BonusAccrualType AccrualType { get; private set; }
    public decimal AccrualValue { get; private set; }
    public decimal MaxPaymentPercent { get; private set; }
    public bool AccrueOnBonusPayment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private BonusSettings()
    {
    }

    public BonusSettings(
        Guid id,
        Guid organizationId,
        bool isEnabled,
        decimal pointValue,
        BonusAccrualType accrualType,
        decimal accrualValue,
        decimal maxPaymentPercent,
        bool accrueOnBonusPayment,
        DateTime createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required", nameof(id));

        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId is required", nameof(organizationId));

        if (createdAt == default)
            throw new ArgumentException("CreatedAt is required", nameof(createdAt));

        Validate(pointValue, accrualType, accrualValue, maxPaymentPercent);

        Id = id;
        OrganizationId = organizationId;
        IsEnabled = isEnabled;
        PointValue = pointValue;
        AccrualType = accrualType;
        AccrualValue = accrualValue;
        MaxPaymentPercent = maxPaymentPercent;
        AccrueOnBonusPayment = accrueOnBonusPayment;
        CreatedAt = createdAt;
    }

    public static BonusSettings CreateDefault(Guid organizationId, DateTime createdAt)
    {
        return new BonusSettings(
            Guid.NewGuid(),
            organizationId,
            isEnabled: false,
            pointValue: 1.00m,
            BonusAccrualType.Percent,
            accrualValue: 0,
            maxPaymentPercent: 0,
            accrueOnBonusPayment: false,
            createdAt);
    }

    public void Update(
        bool isEnabled,
        decimal pointValue,
        BonusAccrualType accrualType,
        decimal accrualValue,
        decimal maxPaymentPercent,
        bool accrueOnBonusPayment,
        DateTime updatedAt)
    {
        if (updatedAt == default)
            throw new ArgumentException("UpdatedAt is required", nameof(updatedAt));

        Validate(pointValue, accrualType, accrualValue, maxPaymentPercent);

        IsEnabled = isEnabled;
        PointValue = pointValue;
        AccrualType = accrualType;
        AccrualValue = accrualValue;
        MaxPaymentPercent = maxPaymentPercent;
        AccrueOnBonusPayment = accrueOnBonusPayment;
        UpdatedAt = updatedAt;
    }

    private static void Validate(
        decimal pointValue,
        BonusAccrualType accrualType,
        decimal accrualValue,
        decimal maxPaymentPercent)
    {
        if (pointValue <= 0)
            throw new ArgumentException("PointValue must be greater than zero", nameof(pointValue));

        if (!Enum.IsDefined(accrualType))
            throw new ArgumentException("Invalid accrual type", nameof(accrualType));

        if (accrualValue < 0)
            throw new ArgumentException("AccrualValue must be greater than or equal to zero", nameof(accrualValue));

        if (accrualType == BonusAccrualType.Percent && accrualValue > 100)
            throw new ArgumentException("Percent AccrualValue cannot exceed 100", nameof(accrualValue));

        if (maxPaymentPercent < 0 || maxPaymentPercent > 100)
            throw new ArgumentException("MaxPaymentPercent must be between 0 and 100", nameof(maxPaymentPercent));
    }
}
