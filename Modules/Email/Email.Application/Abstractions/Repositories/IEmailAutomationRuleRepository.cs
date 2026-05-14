using Email.Domain.Entities;

namespace Email.Application.Abstractions.Repositories;

public interface IEmailAutomationRuleRepository
{
    Task AddAsync(EmailAutomationRule rule, CancellationToken cancellationToken);

    Task<EmailAutomationRule?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);

    Task<List<EmailAutomationRule>> GetEnabledRulesAsync(CancellationToken cancellationToken);
}
