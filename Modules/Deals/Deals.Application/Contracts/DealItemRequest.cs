using Deals.Domain.Enums;

namespace Deals.Application.Contracts;

public sealed record DealItemRequest(
    DealItemType ItemType,
    Guid ItemId,
    Guid? StorageId,
    decimal Quantity,
    DealDiscountType? ManualDiscountType,
    decimal? ManualDiscountValue);
