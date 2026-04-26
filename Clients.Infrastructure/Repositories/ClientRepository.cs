using Clients.Application.Abstractions.Repositories;
using Clients.Domain.Entities;
using Clients.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Clients.Infrastructure.Repositories;

public sealed class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ClientRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken)
    {
        await _dbContext.Set<Client>().AddAsync(client, cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Client>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<Client>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Client>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Client>> SearchAsync(
        Guid organizationId,
        string? search,
        ClientStatus? status,
        ClientSource? source,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Client>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.FirstName, pattern) ||
                EF.Functions.ILike(x.LastName, pattern) ||
                (x.MiddleName != null && EF.Functions.ILike(x.MiddleName, pattern)) ||
                (x.Email != null && EF.Functions.ILike(x.Email, pattern)) ||
                (x.Phone != null && EF.Functions.ILike(x.Phone, pattern)) ||
                (x.Notes != null && EF.Functions.ILike(x.Notes, pattern)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(x => x.Source == source.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Client>()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }
}
