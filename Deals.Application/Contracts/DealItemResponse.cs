using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Contracts;

public sealed record DealItemResponse(
    Guid Id,
    DealItemType ItemType,
    Guid ItemId,
    Guid? StorageId,
    string NameSnapshot,
    decimal Quantity,
    decimal PriceAtMoment,
    DealDiscountType DiscountType,
    decimal? DiscountValue,
    decimal DiscountAmount,
    decimal TotalAmount,
    decimal FinalAmount);

internal static class DealItemResponseMapper
{
    public static DealItemResponse ToResponse(this DealItem item)
    {
        return new DealItemResponse(
            item.Id,
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
            item.FinalAmount);
    }
}
