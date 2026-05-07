using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Contracts;

public sealed record DealReturnResponse(
    Guid Id,
    Guid OrganizationId,
    Guid DealId,
    DealReturnStatus Status,
    string Reason,
    string? CancellationReason,
    decimal TotalAmount,
    decimal BonusPointsReturned,
    decimal BonusAccrualReversed,
    decimal MoneyAmount,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? CompletedAt,
    Guid? CompletedByUserId,
    DateTime? CancelledAt,
    Guid? CancelledByUserId,
    DateTime? UpdatedAt,
    IReadOnlyList<DealReturnItemResponse> Items);

internal static class DealReturnResponseMapper
{
    public static DealReturnResponse ToResponse(this DealReturn dealReturn)
    {
        return new DealReturnResponse(
            dealReturn.Id,
            dealReturn.OrganizationId,
            dealReturn.DealId,
            dealReturn.Status,
            dealReturn.Reason,
            dealReturn.CancellationReason,
            dealReturn.TotalAmount,
            dealReturn.BonusPointsReturned,
            dealReturn.BonusAccrualReversed,
            dealReturn.MoneyAmount,
            dealReturn.CreatedAt,
            dealReturn.CreatedByUserId,
            dealReturn.CompletedAt,
            dealReturn.CompletedByUserId,
            dealReturn.CancelledAt,
            dealReturn.CancelledByUserId,
            dealReturn.UpdatedAt,
            dealReturn.Items
                .OrderBy(item => item.NameSnapshot)
                .ThenBy(item => item.Id)
                .Select(item => item.ToResponse())
                .ToList());
    }
}
