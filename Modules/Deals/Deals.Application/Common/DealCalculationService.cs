using Deals.Domain.Enums;

namespace Deals.Application.Common;

public sealed record DealItemCalculation(
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal FinalAmount);

public sealed record DealCalculation(
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal BonusPointsUsed,
    decimal BonusDiscountAmount,
    decimal FinalAmount);

public sealed class DealCalculationService
{
    public DealItemCalculation CalculateItem(
        decimal quantity,
        decimal priceAtMoment,
        DealDiscountType discountType,
        decimal? discountValue)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (priceAtMoment < 0)
            throw new ArgumentException("PriceAtMoment must be greater than or equal to zero", nameof(priceAtMoment));

        var totalAmount = RoundMoney(quantity * priceAtMoment);
        var discountAmount = discountType switch
        {
            DealDiscountType.None => 0,
            DealDiscountType.Percent => RoundMoney(totalAmount * (discountValue ?? 0) / 100),
            DealDiscountType.Fixed => RoundMoney(discountValue ?? 0),
            _ => throw new ArgumentException("Invalid discount type", nameof(discountType))
        };

        if (discountAmount > totalAmount)
        {
            discountAmount = totalAmount;
        }

        var finalAmount = RoundMoney(totalAmount - discountAmount);

        return new DealItemCalculation(totalAmount, discountAmount, finalAmount);
    }

    public DealCalculation CalculateDeal(
        IEnumerable<DealItemCalculation> itemCalculations,
        decimal bonusPointsUsed,
        decimal bonusDiscountAmount)
    {
        if (bonusPointsUsed < 0)
            throw new ArgumentException("BonusPointsUsed must be greater than or equal to zero", nameof(bonusPointsUsed));

        if (bonusDiscountAmount < 0)
            throw new ArgumentException("BonusDiscountAmount must be greater than or equal to zero", nameof(bonusDiscountAmount));

        var items = itemCalculations.ToList();
        var totalAmount = RoundMoney(items.Sum(x => x.TotalAmount));
        var discountAmount = RoundMoney(items.Sum(x => x.DiscountAmount));
        var remainingAmount = RoundMoney(totalAmount - discountAmount);
        var roundedBonusPoints = RoundPoints(bonusPointsUsed);
        var roundedBonusDiscountAmount = RoundMoney(bonusDiscountAmount);

        if (roundedBonusDiscountAmount > remainingAmount)
        {
            throw new ArgumentException("BonusDiscountAmount cannot exceed remaining amount", nameof(bonusDiscountAmount));
        }

        var finalAmount = RoundMoney(totalAmount - discountAmount - roundedBonusDiscountAmount);

        return new DealCalculation(
            totalAmount,
            discountAmount,
            roundedBonusPoints,
            roundedBonusDiscountAmount,
            finalAmount);
    }

    private static decimal RoundMoney(decimal value)
    {
        return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundPoints(decimal value)
    {
        return decimal.Round(value, 3, MidpointRounding.AwayFromZero);
    }
}
