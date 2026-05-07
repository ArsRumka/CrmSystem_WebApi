namespace Bonus.Application.Abstractions.Lookups;

public interface IBonusClientLookupService
{
    Task<bool> ExistsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken);
}
