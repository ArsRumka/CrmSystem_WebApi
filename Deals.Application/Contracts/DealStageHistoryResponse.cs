using Deals.Domain.Entities;

namespace Deals.Application.Contracts;

public sealed record DealStageHistoryResponse(
    Guid Id,
    Guid DealId,
    Guid? OldStageId,
    Guid NewStageId,
    Guid ChangedByUserId,
    DateTime ChangedAt);

internal static class DealStageHistoryResponseMapper
{
    public static DealStageHistoryResponse ToResponse(this DealStageHistory history)
    {
        return new DealStageHistoryResponse(
            history.Id,
            history.DealId,
            history.OldStageId,
            history.NewStageId,
            history.ChangedByUserId,
            history.ChangedAt);
    }
}
