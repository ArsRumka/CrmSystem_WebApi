using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class ActivationKeyRepository : IActivationKeyRepository
{
    private readonly AppDbContext _dbContext;

    public ActivationKeyRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ActivationKey activationKey, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<ActivationKey>().AddAsync(activationKey, cancellationToken);
    }

    public Task<ActivationKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<ActivationKey>()
            .FirstOrDefaultAsync(x => x.KeyHash == keyHash, cancellationToken);
    }

    public Task<ActivationKey?> GetByRequestIdAsync(Guid organizationRequestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<ActivationKey>()
            .FirstOrDefaultAsync(x => x.OrganizationRequestId == organizationRequestId, cancellationToken);
    }
}
