using Chat.Domain.Entities;

namespace Chat.Application.Abstractions.Repositories;

public interface IChatConversationRepository
{
    Task AddAsync(ChatConversation conversation, CancellationToken cancellationToken);
    Task<ChatConversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ChatConversation?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ChatConversation>> GetForUserAsync(Guid organizationId, Guid userId, bool activeOnly, CancellationToken cancellationToken);
    Task<ChatConversation?> FindDirectConversationAsync(Guid organizationId, Guid userId1, Guid userId2, CancellationToken cancellationToken);
    Task<bool> ActiveInterOrganizationConversationExistsAsync(Guid organizationAId, Guid organizationBId, CancellationToken cancellationToken);
}
