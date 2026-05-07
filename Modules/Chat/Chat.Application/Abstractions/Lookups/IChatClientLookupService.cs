namespace Chat.Application.Abstractions.Lookups;

public interface IChatClientLookupService
{
    Task<bool> ExistsAsync(Guid organizationId, Guid clientId, CancellationToken cancellationToken);
}
