using Deals.Domain.Entities;

namespace Deals.Application.Contracts;

public sealed record DealResponse(
    Guid Id,
    Guid OrganizationId,
    Guid ClientId,
    Guid ResponsibleUserId,
    Guid StageId,
    string? StageName,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal BonusPointsUsed,
    decimal BonusDiscountAmount,
    decimal FinalAmount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ClosedAt,
    string? Notes,
    IReadOnlyList<DealItemResponse> Items,
    IReadOnlyList<DealStageHistoryResponse> StageHistory);

internal static class DealResponseMapper
{
    public static DealResponse ToResponse(this Deal deal, string? stageName)
    {
        return deal.ToResponse(stageName, stageHistoryOverride: null);
    }

    public static DealResponse ToResponse(
        this Deal deal,
        string? stageName,
        IReadOnlyCollection<DealStageHistory>? stageHistoryOverride)
    {
        return new DealResponse(
            deal.Id,
            deal.OrganizationId,
            deal.ClientId,
            deal.ResponsibleUserId,
            deal.StageId,
            stageName,
            deal.TotalAmount,
            deal.DiscountAmount,
            deal.BonusPointsUsed,
            deal.BonusDiscountAmount,
            deal.FinalAmount,
            deal.IsActive,
            deal.CreatedAt,
            deal.UpdatedAt,
            deal.ClosedAt,
            deal.Notes,
            deal.Items.Select(item => item.ToResponse()).ToList(),
            (stageHistoryOverride ?? deal.StageHistory)
                .OrderBy(history => history.ChangedAt)
                .Select(history => history.ToResponse())
                .ToList());
    }
}
