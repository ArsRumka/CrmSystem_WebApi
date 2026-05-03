using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Domain.Entities;

namespace Warehouse.Infrastructure.Repositories;

public sealed class StorageRepository : IStorageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StorageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Storage storage, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Storage>().AddAsync(storage, cancellationToken);
    }

    public Task<Storage?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Storage>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Storage>> SearchAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Storage>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Address != null && EF.Functions.ILike(x.Address, pattern)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Storage>()
            .AnyAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }

    public Task<Storage?> GetDefaultAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Storage>()
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId &&
                     x.IsDefault &&
                     x.IsActive,
                cancellationToken);
    }

    public Task<List<Storage>> GetActiveAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Storage>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task ClearDefaultFlagsAsync(
        Guid organizationId,
        Guid exceptStorageId,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var storages = await _dbContext.Set<Storage>()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.IsDefault &&
                x.Id != exceptStorageId)
            .ToListAsync(cancellationToken);

        foreach (var storage in storages)
        {
            storage.RemoveDefault(updatedAt);
        }
    }

    public Task<bool> HasPositiveStockAsync(
        Guid organizationId,
        Guid storageId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ProductStock>()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.StorageId == storageId &&
                     x.Quantity > 0,
                cancellationToken);
    }
}

