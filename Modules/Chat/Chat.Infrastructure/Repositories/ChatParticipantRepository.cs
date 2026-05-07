using Chat.Application.Abstractions.Repositories;
using Chat.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class ChatParticipantRepository : IChatParticipantRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ChatParticipantRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChatParticipant participant, CancellationToken cancellationToken)
    {
        await _dbContext.Set<ChatParticipant>().AddAsync(participant, cancellationToken);
    }

    public Task<ChatParticipant?> GetAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatParticipant>()
            .FirstOrDefaultAsync(
                x => x.ConversationId == conversationId && x.UserId == userId,
                cancellationToken);
    }

    public async Task<List<ChatParticipant>> GetActiveByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<ChatParticipant>()
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.IsActive)
            .OrderBy(x => x.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsActiveParticipantAsync(
        Guid conversationId,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatParticipant>()
            .AsNoTracking()
            .AnyAsync(
                x => x.ConversationId == conversationId &&
                     x.OrganizationId == organizationId &&
                     x.UserId == userId &&
                     x.IsActive,
                cancellationToken);
    }

    public Task<int> CountActiveParticipantsAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatParticipant>()
            .CountAsync(x => x.ConversationId == conversationId && x.IsActive, cancellationToken);
    }

    public Task<int> CountActiveParticipantsByOrganizationAsync(
        Guid conversationId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatParticipant>()
            .CountAsync(
                x => x.ConversationId == conversationId &&
                     x.OrganizationId == organizationId &&
                     x.IsActive,
                cancellationToken);
    }
}
