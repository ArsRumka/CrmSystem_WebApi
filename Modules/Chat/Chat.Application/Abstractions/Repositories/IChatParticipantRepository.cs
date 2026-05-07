using Chat.Domain.Entities;

namespace Chat.Application.Abstractions.Repositories;

public interface IChatParticipantRepository
{
    Task AddAsync(ChatParticipant participant, CancellationToken cancellationToken);
    Task<ChatParticipant?> GetAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);
    Task<List<ChatParticipant>> GetActiveByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<bool> IsActiveParticipantAsync(Guid conversationId, Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<int> CountActiveParticipantsAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<int> CountActiveParticipantsByOrganizationAsync(Guid conversationId, Guid organizationId, CancellationToken cancellationToken);
}
