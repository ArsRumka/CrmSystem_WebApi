using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Application.Contracts;

public sealed record CategoryResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    Guid? ParentCategoryId,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class CategoryResponseMapper
{
    public static CategoryResponse ToResponse(this Category category)
    {
        return new CategoryResponse(
            category.Id,
            category.OrganizationId,
            category.Name,
            category.ParentCategoryId,
            category.BonusType,
            category.BonusValue,
            category.DiscountType,
            category.DiscountValue,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
