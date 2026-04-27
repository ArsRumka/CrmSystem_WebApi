using Deals.Application.Abstractions.Repositories;
using Deals.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Deals.Infrastructure.Repositories;

public sealed class DealStageRepository : IDealStageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DealStageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DealStage stage, CancellationToken cancellationToken)
    {
        await _dbContext.Set<DealStage>().AddAsync(stage, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DealStage> stages, CancellationToken cancellationToken)
    {
        await _dbContext.Set<DealStage>().AddRangeAsync(stages, cancellationToken);
    }

    public Task<DealStage?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<DealStage>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<DealStage>> GetByOrganizationIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<DealStage>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DealStage>> SearchAsync(
        Guid organizationId,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<DealStage>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<DealStage>()
            .AnyAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }

    public Task<DealStage?> GetInitialStageAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<DealStage>()
            .Where(x => x.OrganizationId == organizationId && x.IsActive && !x.IsFinal)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
