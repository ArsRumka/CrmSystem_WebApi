using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Product>().AddAsync(product, cancellationToken);
    }

    public Task<Product?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Product>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? categoryId,
        bool? isActive,
        BonusType? bonusType,
        DiscountType? discountType,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Product>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Description != null && EF.Functions.ILike(x.Description, pattern)) ||
                (x.Sku != null && EF.Functions.ILike(x.Sku, pattern)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (bonusType.HasValue)
        {
            query = query.Where(x => x.BonusType == bonusType.Value);
        }

        if (discountType.HasValue)
        {
            query = query.Where(x => x.DiscountType == discountType.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Sku)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Product>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }
}
