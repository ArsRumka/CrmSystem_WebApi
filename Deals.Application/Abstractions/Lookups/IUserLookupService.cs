namespace Deals.Application.Abstractions.Lookups;

public interface IUserLookupService
{
    Task<bool> ExistsActiveAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
}
