using Chat.Domain.Entities;
using Chat.Domain.Enums;

namespace Chat.Application.Abstractions.Repositories;

public interface IChatContactRequestRepository
{
    Task AddAsync(ChatContactRequest request, CancellationToken cancellationToken);
    Task<ChatContactRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<ChatContactRequest>> GetIncomingAsync(Guid organizationId, ChatContactRequestStatus? status, CancellationToken cancellationToken);
    Task<List<ChatContactRequest>> GetOutgoingAsync(Guid organizationId, ChatContactRequestStatus? status, CancellationToken cancellationToken);
    Task<bool> PendingExistsAsync(Guid requesterOrganizationId, Guid targetOrganizationId, CancellationToken cancellationToken);
}
