using Catalog.Domain.Entities;
using CatalogDiscountType = Catalog.Domain.Enums.DiscountType;
using Deals.Application.Abstractions.Lookups;
using Deals.Application.Contracts;
using Deals.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Lookups;

public sealed class CatalogLookupService : ICatalogLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public CatalogLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CatalogItemSnapshot?> GetItemSnapshotAsync(
        Guid organizationId,
        DealItemType itemType,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return itemType switch
        {
            DealItemType.Product => await GetProductSnapshotAsync(organizationId, itemId, cancellationToken),
            DealItemType.Service => await GetServiceSnapshotAsync(organizationId, itemId, cancellationToken),
            _ => null
        };
    }

    private async Task<CatalogItemSnapshot?> GetProductSnapshotAsync(
        Guid organizationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Set<Product>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == itemId,
                cancellationToken);

        if (product is null)
        {
            return null;
        }

        var discount = await ResolveDiscountAsync(
            organizationId,
            product.DiscountType,
            product.DiscountValue,
            product.CategoryId,
            cancellationToken);

        return new CatalogItemSnapshot(
            product.Id,
            DealItemType.Product,
            product.Name,
            product.Price,
            discount.Type,
            discount.Value,
            product.IsActive);
    }

    private async Task<CatalogItemSnapshot?> GetServiceSnapshotAsync(
        Guid organizationId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var service = await _dbContext.Set<Service>()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == itemId,
                cancellationToken);

        if (service is null)
        {
            return null;
        }

        var discount = await ResolveDiscountAsync(
            organizationId,
            service.DiscountType,
            service.DiscountValue,
            service.CategoryId,
            cancellationToken);

        return new CatalogItemSnapshot(
            service.Id,
            DealItemType.Service,
            service.Name,
            service.Price,
            discount.Type,
            discount.Value,
            service.IsActive);
    }

    private async Task<(DealDiscountType Type, decimal? Value)> ResolveDiscountAsync(
        Guid organizationId,
        CatalogDiscountType discountType,
        decimal? discountValue,
        Guid? categoryId,
        CancellationToken cancellationToken)
    {
        if (discountType != CatalogDiscountType.Inherit)
        {
            return MapDiscount(discountType, discountValue);
        }

        var currentCategoryId = categoryId;

        while (currentCategoryId.HasValue)
        {
            var category = await _dbContext.Set<Category>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == currentCategoryId.Value)
                .Select(x => new
                {
                    x.ParentCategoryId,
                    x.DiscountType,
                    x.DiscountValue
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (category is null)
            {
                return (DealDiscountType.None, null);
            }

            if (category.DiscountType != CatalogDiscountType.Inherit)
            {
                return MapDiscount(category.DiscountType, category.DiscountValue);
            }

            currentCategoryId = category.ParentCategoryId;
        }

        return (DealDiscountType.None, null);
    }

    private static (DealDiscountType Type, decimal? Value) MapDiscount(
        CatalogDiscountType discountType,
        decimal? discountValue)
    {
        return discountType switch
        {
            CatalogDiscountType.Percent => (DealDiscountType.Percent, discountValue),
            CatalogDiscountType.Fixed => (DealDiscountType.Fixed, discountValue),
            CatalogDiscountType.None or CatalogDiscountType.Inherit => (DealDiscountType.None, null),
            _ => (DealDiscountType.None, null)
        };
    }
}
