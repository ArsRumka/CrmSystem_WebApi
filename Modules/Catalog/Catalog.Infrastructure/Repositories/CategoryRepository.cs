using Catalog.Application.Abstractions.Repositories;
using Catalog.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CategoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Category>().AddAsync(category, cancellationToken);
    }

    public Task<Category?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Category>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Category>> SearchAsync(
        Guid organizationId,
        string? search,
        Guid? parentCategoryId,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Category>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        if (parentCategoryId.HasValue)
        {
            query = query.Where(x => x.ParentCategoryId == parentCategoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Category>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<bool> WouldCreateCycleAsync(
        Guid organizationId,
        Guid categoryId,
        Guid? newParentCategoryId,
        CancellationToken cancellationToken)
    {
        var currentParentId = newParentCategoryId;

        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == categoryId)
            {
                return true;
            }

            var parent = await _dbContext.Set<Category>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.Id == currentParentId.Value)
                .Select(x => new { x.ParentCategoryId })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
            {
                return false;
            }

            currentParentId = parent.ParentCategoryId;
        }

        return false;
    }
}
