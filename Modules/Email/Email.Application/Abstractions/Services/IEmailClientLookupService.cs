namespace Email.Application.Abstractions.Services;

public interface IEmailClientLookupService
{
    Task<List<EmailClientInfo>> GetClientsByIdsAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken);

    Task<List<InactiveClientEmailCandidate>> GetInactiveClientsAsync(
        Guid organizationId,
        int inactivityDays,
        int repeatAfterDays,
        CancellationToken cancellationToken);
}

public sealed record EmailClientInfo(
    Guid ClientId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string FullName,
    string? Email,
    bool IsActive,
    bool AllowMarketingEmails);

public sealed record InactiveClientEmailCandidate(
    Guid ClientId,
    string FirstName,
    string LastName,
    string? MiddleName,
    string FullName,
    string? Email,
    bool AllowMarketingEmails,
    DateTime LastDealDate,
    int DaysSinceLastDeal);
