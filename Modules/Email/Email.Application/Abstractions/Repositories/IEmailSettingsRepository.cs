using Email.Domain.Entities;

namespace Email.Application.Abstractions.Repositories;

public interface IEmailSettingsRepository
{
    Task AddAsync(EmailSettings settings, CancellationToken cancellationToken);

    Task<EmailSettings?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
}
