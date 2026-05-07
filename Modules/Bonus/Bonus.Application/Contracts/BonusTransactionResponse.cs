using Bonus.Domain.Entities;
using Bonus.Domain.Enums;

namespace Bonus.Application.Contracts;

public sealed record BonusTransactionResponse(
    Guid Id,
    Guid OrganizationId,
    Guid BonusAccountId,
    Guid ClientId,
    Guid? DealId,
    Guid? SourceReturnId,
    BonusTransactionType Type,
    decimal Points,
    decimal MonetaryAmount,
    decimal PointValueAtMoment,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Reason,
    DateTime CreatedAt,
    Guid? CreatedByUserId);

internal static class BonusTransactionResponseMapper
{
    public static BonusTransactionResponse ToResponse(this BonusTransaction transaction)
    {
        return new BonusTransactionResponse(
            transaction.Id,
            transaction.OrganizationId,
            transaction.BonusAccountId,
            transaction.ClientId,
            transaction.DealId,
            transaction.SourceReturnId,
            transaction.Type,
            transaction.Points,
            transaction.MonetaryAmount,
            transaction.PointValueAtMoment,
            transaction.BalanceBefore,
            transaction.BalanceAfter,
            transaction.Reason,
            transaction.CreatedAt,
            transaction.CreatedByUserId);
    }
}
