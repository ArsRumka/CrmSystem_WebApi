using Deals.Domain.Entities;

namespace Deals.Application.Contracts;

public sealed record DealStageResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    int Order,
    bool IsFinal,
    bool IsSuccessful,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class DealStageResponseMapper
{
    public static DealStageResponse ToResponse(this DealStage stage)
    {
        return new DealStageResponse(
            stage.Id,
            stage.OrganizationId,
            stage.Name,
            stage.Order,
            stage.IsFinal,
            stage.IsSuccessful,
            stage.IsActive,
            stage.CreatedAt,
            stage.UpdatedAt);
    }
}
