namespace Bonus.Application.Abstractions.Services;

public sealed record BonusDealReturnResult(
    decimal BonusPointsReturned,
    decimal BonusAccrualReversed);

public interface IBonusDealReturnService
{
    Task<BonusDealReturnResult> ProcessReturnAsync(
        Guid organizationId,
        Guid dealId,
        Guid dealReturnId,
        Guid userId,
        decimal returnRatio,
        decimal returnAmount,
        decimal bonusDiscountMoneyShare,
        string reason,
        CancellationToken cancellationToken);
}
