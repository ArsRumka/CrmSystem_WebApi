using Chat.Application.Abstractions.Repositories;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class ChatContactRequestRepository : IChatContactRequestRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ChatContactRequestRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChatContactRequest request, CancellationToken cancellationToken)
    {
        await _dbContext.Set<ChatContactRequest>().AddAsync(request, cancellationToken);
    }

    public Task<ChatContactRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatContactRequest>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ChatContactRequest>> GetIncomingAsync(
        Guid organizationId,
        ChatContactRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatContactRequest>()
            .AsNoTracking()
            .Where(x => x.TargetOrganizationId == organizationId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ChatContactRequest>> GetOutgoingAsync(
        Guid organizationId,
        ChatContactRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatContactRequest>()
            .AsNoTracking()
            .Where(x => x.RequesterOrganizationId == organizationId);

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> PendingExistsAsync(
        Guid requesterOrganizationId,
        Guid targetOrganizationId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatContactRequest>()
            .AsNoTracking()
            .AnyAsync(
                x => x.RequesterOrganizationId == requesterOrganizationId &&
                     x.TargetOrganizationId == targetOrganizationId &&
                     x.Status == ChatContactRequestStatus.Pending,
                cancellationToken);
    }
}
