using Bonus.Domain.Entities;
using Bonus.Domain.Enums;

namespace Bonus.Application.Common;

internal static class BonusAuditSnapshots
{
    public static object Settings(BonusSettings settings)
    {
        return new
        {
            settings.IsEnabled,
            settings.PointValue,
            settings.AccrualType,
            settings.AccrualValue,
            settings.MaxPaymentPercent,
            settings.AccrueOnBonusPayment
        };
    }

    public static object ManualAdjustment(
        BonusAccount account,
        decimal pointsDelta,
        decimal balanceBefore,
        BonusTransactionType transactionType,
        string reason)
    {
        return new
        {
            account.ClientId,
            PointsDelta = pointsDelta,
            BalanceBefore = balanceBefore,
            BalanceAfter = account.Balance,
            TransactionType = transactionType,
            Reason = reason
        };
    }
}
