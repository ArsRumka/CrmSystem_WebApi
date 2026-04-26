using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Catalog.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class ServiceRepository : IServiceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ServiceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Service service, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Service>().AddAsync(service, cancellationToken);
    }

    public Task<Service?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Service>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Service>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? categoryId,
        bool? isActive,
        BonusType? bonusType,
        DiscountType? discountType,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Service>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                (x.Description != null && EF.Functions.ILike(x.Description, pattern)));
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
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Service>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }
}
