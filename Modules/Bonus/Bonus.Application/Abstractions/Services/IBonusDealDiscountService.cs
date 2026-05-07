using Bonus.Application.Contracts;

namespace Bonus.Application.Abstractions.Services;

public interface IBonusDealDiscountService
{
    Task<DealBonusDiscountResult> CalculateAsync(
        Guid organizationId,
        Guid clientId,
        decimal amountBeforeBonus,
        decimal requestedBonusPoints,
        CancellationToken cancellationToken);
}
