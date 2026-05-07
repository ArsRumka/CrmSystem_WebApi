using BuildingBlocks.Application.Exceptions;
using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Common;

internal static class DealReturnGuards
{
    public static void EnsureReturnableDeal(Deal deal, DealStage stage)
    {
        if (!deal.IsActive)
        {
            throw new ConflictException("Inactive deals cannot be returned");
        }

        if (!stage.IsFinal || !stage.IsSuccessful)
        {
            throw new ConflictException("Only successful final-stage deals can be returned");
        }
    }

    public static void EnsureDraft(DealReturn dealReturn)
    {
        if (dealReturn.Status != DealReturnStatus.Draft)
        {
            throw new ConflictException("Only draft returns can be changed");
        }
    }
}
