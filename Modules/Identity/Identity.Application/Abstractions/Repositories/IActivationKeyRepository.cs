using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IActivationKeyRepository
{
    Task AddAsync(ActivationKey activationKey, CancellationToken cancellationToken = default);

    Task<ActivationKey?> GetByHashAsync(string keyHash, CancellationToken cancellationToken = default);

    Task<ActivationKey?> GetByRequestIdAsync(Guid organizationRequestId, CancellationToken cancellationToken = default);
}
