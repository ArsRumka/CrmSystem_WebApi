using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class OrganizationRequestRepository : IOrganizationRequestRepository
{
    private readonly AppDbContext _dbContext;

    public OrganizationRequestRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OrganizationRequest organizationRequest, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<OrganizationRequest>().AddAsync(organizationRequest, cancellationToken);
    }

    public Task<OrganizationRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<OrganizationRequest>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OrganizationRequest>()
            .Where(x => x.Status == OrganizationRequestStatus.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationRequest>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OrganizationRequest>()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
