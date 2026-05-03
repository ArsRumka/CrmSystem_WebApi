using Catalog.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Services;

namespace Warehouse.Infrastructure.Services;

public sealed class WarehouseProductLookupService : IWarehouseProductLookupService
{
    private readonly ApplicationDbContext _dbContext;

    public WarehouseProductLookupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Product>()
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == productId, cancellationToken);
    }

    public Task<bool> ExistsActiveAsync(Guid organizationId, Guid productId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Product>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.Id == productId &&
                     x.IsActive,
                cancellationToken);
    }
}

