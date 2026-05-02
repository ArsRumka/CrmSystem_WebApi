using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Application.Contracts;

public sealed record ProductResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? CategoryId,
    string Name,
    string? Sku,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class ProductResponseMapper
{
    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse(
            product.Id,
            product.OrganizationId,
            product.CategoryId,
            product.Name,
            product.Sku,
            product.Description,
            product.Price,
            product.BonusType,
            product.BonusValue,
            product.DiscountType,
            product.DiscountValue,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }
}
