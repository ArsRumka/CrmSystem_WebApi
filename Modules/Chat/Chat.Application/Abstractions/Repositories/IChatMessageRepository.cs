using Chat.Domain.Entities;

namespace Chat.Application.Abstractions.Repositories;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<ChatMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ChatMessage>> GetByConversationIdAsync(Guid conversationId, DateTime? before, int limit, CancellationToken cancellationToken);
    Task<ChatMessage?> GetLastMessageAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<int> CountUnreadAsync(Guid conversationId, Guid userId, DateTime? lastReadAt, CancellationToken cancellationToken);
}
