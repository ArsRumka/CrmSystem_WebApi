namespace Chat.Application.Abstractions.Lookups;

public interface IChatDealLookupService
{
    Task<bool> ExistsAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken);
}
