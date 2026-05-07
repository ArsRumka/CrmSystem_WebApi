using Chat.Application.Abstractions.Repositories;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class ChatConversationRepository : IChatConversationRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ChatConversationRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChatConversation conversation, CancellationToken cancellationToken)
    {
        await _dbContext.Set<ChatConversation>().AddAsync(conversation, cancellationToken);
    }

    public Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatConversation>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ChatConversation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatConversation>()
            .Include(x => x.Organizations)
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ChatConversation>> GetForUserAsync(
        Guid organizationId,
        Guid userId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatConversation>()
            .AsNoTracking()
            .Include(x => x.Organizations)
            .Include(x => x.Participants)
            .Where(x => x.Participants.Any(p =>
                p.OrganizationId == organizationId &&
                p.UserId == userId));

        if (activeOnly)
        {
            query = query.Where(x =>
                x.IsActive &&
                x.DeletedAt == null &&
                x.Participants.Any(p =>
                    p.OrganizationId == organizationId &&
                    p.UserId == userId &&
                    p.IsActive));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ChatConversation?> FindDirectConversationAsync(
        Guid organizationId,
        Guid userId1,
        Guid userId2,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatConversation>()
            .Include(x => x.Organizations)
            .Include(x => x.Participants)
            .Where(x =>
                x.Type == ChatConversationType.Direct &&
                x.OwnerOrganizationId == organizationId &&
                x.IsActive &&
                x.DeletedAt == null)
            .Where(x => x.Participants.Count(p => p.IsActive) == 2)
            .Where(x => x.Participants.Any(p =>
                p.OrganizationId == organizationId &&
                p.UserId == userId1 &&
                p.IsActive))
            .Where(x => x.Participants.Any(p =>
                p.OrganizationId == organizationId &&
                p.UserId == userId2 &&
                p.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ActiveInterOrganizationConversationExistsAsync(
        Guid organizationAId,
        Guid organizationBId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatConversation>()
            .AsNoTracking()
            .Where(x =>
                x.Type == ChatConversationType.InterOrganization &&
                x.IsActive &&
                x.DeletedAt == null)
            .Where(x => x.Organizations.Count(o => o.IsActive) == 2)
            .AnyAsync(x =>
                    x.Organizations.Any(o => o.OrganizationId == organizationAId && o.IsActive) &&
                    x.Organizations.Any(o => o.OrganizationId == organizationBId && o.IsActive),
                cancellationToken);
    }
}
