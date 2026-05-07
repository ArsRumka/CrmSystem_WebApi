using Chat.Application.Abstractions.Repositories;
using Chat.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chat.Infrastructure.Repositories;

public sealed class ChatMessageRepository : IChatMessageRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ChatMessageRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        await _dbContext.Set<ChatMessage>().AddAsync(message, cancellationToken);
    }

    public Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatMessage>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<List<ChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        DateTime? before,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatMessage>()
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId);

        if (before.HasValue)
        {
            query = query.Where(x => x.CreatedAt < before.Value);
        }

        var messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(x => x.CreatedAt)
            .ToList();
    }

    public Task<ChatMessage?> GetLastMessageAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<ChatMessage>()
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        Guid conversationId,
        Guid userId,
        DateTime? lastReadAt,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<ChatMessage>()
            .AsNoTracking()
            .Where(x =>
                x.ConversationId == conversationId &&
                x.SenderUserId != userId &&
                !x.IsDeleted);

        if (lastReadAt.HasValue)
        {
            query = query.Where(x => x.CreatedAt > lastReadAt.Value);
        }

        return query.CountAsync(cancellationToken);
    }
}
