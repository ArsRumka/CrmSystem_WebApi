using Bonus.Domain.Entities;
using Bonus.Domain.Enums;

namespace Bonus.Application.Contracts;

public sealed record BonusSettingsResponse(
    Guid Id,
    Guid OrganizationId,
    bool IsEnabled,
    decimal PointValue,
    BonusAccrualType AccrualType,
    decimal AccrualValue,
    decimal MaxPaymentPercent,
    bool AccrueOnBonusPayment,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class BonusSettingsResponseMapper
{
    public static BonusSettingsResponse ToResponse(this BonusSettings settings)
    {
        return new BonusSettingsResponse(
            settings.Id,
            settings.OrganizationId,
            settings.IsEnabled,
            settings.PointValue,
            settings.AccrualType,
            settings.AccrualValue,
            settings.MaxPaymentPercent,
            settings.AccrueOnBonusPayment,
            settings.CreatedAt,
            settings.UpdatedAt);
    }
}
