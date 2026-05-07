namespace Chat.Application.Abstractions.Lookups;

public interface IChatUserLookupService
{
    Task<bool> ExistsActiveInOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<string?> GetUserDisplayNameAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
}
