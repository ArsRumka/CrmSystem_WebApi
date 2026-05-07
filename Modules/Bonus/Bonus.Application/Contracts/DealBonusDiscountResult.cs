namespace Bonus.Application.Contracts;

public sealed record DealBonusDiscountResult(
    decimal AppliedBonusPoints,
    decimal BonusDiscountAmount);
