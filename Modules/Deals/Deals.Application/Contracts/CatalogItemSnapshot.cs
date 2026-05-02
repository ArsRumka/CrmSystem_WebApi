using Deals.Domain.Enums;

namespace Deals.Application.Contracts;

public sealed record CatalogItemSnapshot(
    Guid ItemId,
    DealItemType ItemType,
    string Name,
    decimal Price,
    DealDiscountType DiscountType,
    decimal? DiscountValue,
    bool IsActive);
