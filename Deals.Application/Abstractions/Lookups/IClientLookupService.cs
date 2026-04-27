namespace Deals.Application.Abstractions.Lookups;

public interface IClientLookupService
{
    Task<bool> ExistsActiveAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken);
}
