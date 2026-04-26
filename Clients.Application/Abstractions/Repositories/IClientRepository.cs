using Clients.Domain.Entities;
using Clients.Domain.Enums;

namespace Clients.Application.Abstractions.Repositories;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken);

    Task<Client?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<Client>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<List<Client>> SearchAsync(
        Guid organizationId,
        string? search,
        ClientStatus? status,
        ClientSource? source,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<bool> ExistsByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);
}
