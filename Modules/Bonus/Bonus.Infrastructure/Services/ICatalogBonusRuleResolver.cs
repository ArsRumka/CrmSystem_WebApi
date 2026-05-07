using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using Deals.Domain.Enums;

namespace Bonus.Infrastructure.Services;

internal sealed record ResolvedBonusRule(BonusAccrualType AccrualType, decimal AccrualValue);

internal interface ICatalogBonusRuleResolver
{
    Task<ResolvedBonusRule> ResolveAsync(
        Guid organizationId,
        DealItemType itemType,
        Guid itemId,
        BonusSettings fallbackSettings,
        CancellationToken cancellationToken);
}
