using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using Catalog.Domain.Entities;
using CatalogBonusType = Catalog.Domain.Enums.BonusType;
using Deals.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bonus.Infrastructure.Services;

internal sealed class CatalogBonusRuleResolver : ICatalogBonusRuleResolver
{
    private const int MaxCategoryDepth = 32;

    private readonly ApplicationDbContext _dbContext;

    public CatalogBonusRuleResolver(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResolvedBonusRule> ResolveAsync(
        Guid organizationId,
        DealItemType itemType,
        Guid itemId,
        BonusSettings fallbackSettings,
        CancellationToken cancellationToken)
    {
        return itemType switch
        {
            DealItemType.Product => await ResolveProductAsync(organizationId, itemId, fallbackSettings, cancellationToken),
            DealItemType.Service => await ResolveServiceAsync(organizationId, itemId, fallbackSettings, cancellationToken),
            _ => ToFallback(fallbackSettings)
        };
    }

    private async Task<ResolvedBonusRule> ResolveProductAsync(
        Guid organizationId,
        Guid itemId,
        BonusSettings fallbackSettings,
        CancellationToken cancellationToken)
    {
        var product = await _dbContext.Set<Product>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Id == itemId)
            .Select(x => new
            {
                x.CategoryId,
                x.BonusType,
                x.BonusValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return ToFallback(fallbackSettings);
        }

        if (product.BonusType != CatalogBonusType.Inherit)
        {
            return Map(product.BonusType, product.BonusValue);
        }

        return await ResolveCategoryAsync(organizationId, product.CategoryId, fallbackSettings, cancellationToken);
    }

    private async Task<ResolvedBonusRule> ResolveServiceAsync(
        Guid organizationId,
        Guid itemId,
        BonusSettings fallbackSettings,
        CancellationToken cancellationToken)
    {
        var service = await _dbContext.Set<Service>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Id == itemId)
            .Select(x => new
            {
                x.CategoryId,
                x.BonusType,
                x.BonusValue
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            return ToFallback(fallbackSettings);
        }

        if (service.BonusType != CatalogBonusType.Inherit)
        {
            return Map(service.BonusType, service.BonusValue);
        }

        return await ResolveCategoryAsync(organizationId, service.CategoryId, fallbackSettings, cancellationToken);
    }

    private async Task<ResolvedBonusRule> ResolveCategoryAsync(
        Guid organizationId,
        Guid? categoryId,
        BonusSettings fallbackSettings,
        CancellationToken cancellationToken)
    {
        var currentCategoryId = categoryId;
        var visited = new HashSet<Guid>();
        var depth = 0;

        while (currentCategoryId.HasValue && depth < MaxCategoryDepth)
        {
            if (!visited.Add(currentCategoryId.Value))
            {
                return ToFallback(fallbackSettings);
            }

            var category = await _dbContext.Set<Category>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == currentCategoryId.Value)
                .Select(x => new
                {
                    x.ParentCategoryId,
                    x.BonusType,
                    x.BonusValue
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (category is null)
            {
                return ToFallback(fallbackSettings);
            }

            if (category.BonusType != CatalogBonusType.Inherit)
            {
                return Map(category.BonusType, category.BonusValue);
            }

            currentCategoryId = category.ParentCategoryId;
            depth++;
        }

        return ToFallback(fallbackSettings);
    }

    private static ResolvedBonusRule Map(CatalogBonusType type, decimal? value)
    {
        return type switch
        {
            CatalogBonusType.Percent => new ResolvedBonusRule(BonusAccrualType.Percent, value ?? 0),
            CatalogBonusType.Fixed => new ResolvedBonusRule(BonusAccrualType.Fixed, value ?? 0),
            CatalogBonusType.None or CatalogBonusType.Inherit => new ResolvedBonusRule(BonusAccrualType.None, 0),
            _ => new ResolvedBonusRule(BonusAccrualType.None, 0)
        };
    }

    private static ResolvedBonusRule ToFallback(BonusSettings settings)
    {
        return new ResolvedBonusRule(settings.AccrualType, settings.AccrualValue);
    }
}
