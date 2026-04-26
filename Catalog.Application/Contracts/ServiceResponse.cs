using Catalog.Domain.Entities;
using Catalog.Domain.Enums;

namespace Catalog.Application.Contracts;

public sealed record ServiceResponse(
    Guid Id,
    Guid OrganizationId,
    Guid? CategoryId,
    string Name,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class ServiceResponseMapper
{
    public static ServiceResponse ToResponse(this Service service)
    {
        return new ServiceResponse(
            service.Id,
            service.OrganizationId,
            service.CategoryId,
            service.Name,
            service.Description,
            service.Price,
            service.BonusType,
            service.BonusValue,
            service.DiscountType,
            service.DiscountValue,
            service.IsActive,
            service.CreatedAt,
            service.UpdatedAt);
    }
}
