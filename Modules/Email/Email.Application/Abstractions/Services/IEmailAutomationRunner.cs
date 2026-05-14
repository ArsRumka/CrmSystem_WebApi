using Email.Application.Contracts;

namespace Email.Application.Abstractions.Services;

public interface IEmailAutomationRunner
{
    Task<EmailAutomationRunResponse> RunForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken);

    Task RunAllAsync(CancellationToken cancellationToken);
}
