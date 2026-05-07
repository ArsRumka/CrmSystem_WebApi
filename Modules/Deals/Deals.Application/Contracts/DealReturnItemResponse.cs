using Deals.Domain.Entities;
using Deals.Domain.Enums;

namespace Deals.Application.Contracts;

public sealed record DealReturnItemResponse(
    Guid Id,
    Guid OrganizationId,
    Guid DealReturnId,
    Guid DealId,
    Guid DealItemId,
    DealItemType ItemType,
    Guid ItemId,
    Guid? StorageId,
    string NameSnapshot,
    decimal Quantity,
    decimal ReturnAmount);

internal static class DealReturnItemResponseMapper
{
    public static DealReturnItemResponse ToResponse(this DealReturnItem item)
    {
        return new DealReturnItemResponse(
            item.Id,
            item.OrganizationId,
            item.DealReturnId,
            item.DealId,
            item.DealItemId,
            item.ItemType,
            item.ItemId,
            item.StorageId,
            item.NameSnapshot,
            item.Quantity,
            item.ReturnAmount);
    }
}
