using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Infrastructure.Repositories;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StockMovementRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(StockMovement movement, CancellationToken cancellationToken)
    {
        await _dbContext.Set<StockMovement>().AddAsync(movement, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<StockMovement> movements, CancellationToken cancellationToken)
    {
        await _dbContext.Set<StockMovement>().AddRangeAsync(movements, cancellationToken);
    }

    public Task<StockMovement?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsSaleByDealIdAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.DealId == dealId &&
                     x.Type == StockMovementType.Sale,
                cancellationToken);
    }

    public async Task<List<StockMovement>> SearchAsync(
        Guid organizationId,
        Guid? storageId,
        Guid? productId,
        Guid? dealId,
        StockMovementType? type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<StockMovement>()
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

        if (dealId.HasValue)
        {
            query = query.Where(x => x.DealId == dealId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= dateTo.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

