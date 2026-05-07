namespace Chat.Application.Abstractions.Lookups;

public interface IChatOrganizationLookupService
{
    Task<Guid?> GetOrganizationIdByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsActiveAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<string?> GetOrganizationEmailAsync(Guid organizationId, CancellationToken cancellationToken);
}
