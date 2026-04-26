using Catalog.Domain.Enums;

namespace Catalog.Application.Common;

internal static class CatalogValidationRules
{
    public static bool IsValidBonusRule(BonusType type, decimal? value)
    {
        return type switch
        {
            BonusType.Percent => value.HasValue && value.Value > 0 && value.Value <= 100,
            BonusType.Fixed => value.HasValue && value.Value > 0,
            BonusType.None or BonusType.Inherit => !value.HasValue || value.Value == 0,
            _ => false
        };
    }

    public static bool IsValidDiscountRule(DiscountType type, decimal? value)
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
