using Deals.Domain.Entities;

namespace Deals.Application.Common;

internal static class DealAuditSnapshots
{
    public static object Deal(Deal deal)
    {
        return new
        {
            deal.ClientId,
            deal.ResponsibleUserId,
            deal.StageId,
            deal.TotalAmount,
            deal.DiscountAmount,
            deal.BonusPointsUsed,
            deal.BonusDiscountAmount,
            deal.FinalAmount,
            deal.IsActive,
            Items = deal.Items.Select(DealItem).ToList()
        };
    }

    public static object DealItem(DealItem item)
    {
        return new
        {
            item.ItemType,
            item.ItemId,
            item.StorageId,
            item.NameSnapshot,
            item.Quantity,
            item.PriceAtMoment,
            item.DiscountType,
            item.DiscountValue,
            item.DiscountAmount,
            item.TotalAmount,
            item.FinalAmount
        };
    }

    public static object DealReturn(DealReturn dealReturn)
    {
        return new
        {
            dealReturn.DealId,
            dealReturn.Status,
            dealReturn.TotalAmount,
            dealReturn.MoneyAmount,
            dealReturn.BonusPointsReturned,
            dealReturn.BonusAccrualReversed,
            Items = dealReturn.Items.Select(DealReturnItem).ToList()
        };
    }

    private static object DealReturnItem(DealReturnItem item)
    {
        return new
        {
            item.DealItemId,
            item.ItemType,
            item.ItemId,
            item.StorageId,
            item.NameSnapshot,
            item.Quantity,
            item.ReturnAmount
        };
    }
}
