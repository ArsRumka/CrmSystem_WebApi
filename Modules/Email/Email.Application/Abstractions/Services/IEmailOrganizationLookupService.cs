namespace Email.Application.Abstractions.Services;

public interface IEmailOrganizationLookupService
{
    Task<string?> GetOrganizationNameAsync(Guid organizationId, CancellationToken cancellationToken);
}
