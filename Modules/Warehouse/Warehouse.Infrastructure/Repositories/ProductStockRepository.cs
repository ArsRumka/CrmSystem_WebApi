using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Repositories;

public sealed class ProductStockRepository : IProductStockRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductStockRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ProductStock stock, CancellationToken cancellationToken)
    {
        await _dbContext.Set<ProductStock>().AddAsync(stock, cancellationToken);
    }

    public Task<ProductStock?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ProductStock>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public Task<ProductStock?> GetByStorageAndProductAsync(
        Guid organizationId,
        Guid storageId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ProductStock>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId &&
                     x.StorageId == storageId &&
                     x.ProductId == productId,
                cancellationToken);
    }

    public async Task<List<ProductStock>> SearchAsync(
        Guid organizationId,
        Guid? storageId,
        Guid? productId,
        bool onlyPositive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ProductStock>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (storageId.HasValue)
        {
            query = query.Where(x => x.StorageId == storageId.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(x => x.ProductId == productId.Value);
        }

        if (onlyPositive)
        {
            query = query.Where(x => x.Quantity > 0);
        }

        return await query
            .OrderBy(x => x.StorageId)
            .ThenBy(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<ProductStock>> GetByStorageIdAsync(
        Guid organizationId,
        Guid storageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ProductStock>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.StorageId == storageId)
            .OrderBy(x => x.ProductId)
            .ToListAsync(cancellationToken);
    }
}

