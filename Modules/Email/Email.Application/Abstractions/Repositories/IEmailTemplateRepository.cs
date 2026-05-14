using Email.Domain.Entities;

namespace Email.Application.Abstractions.Repositories;

public interface IEmailTemplateRepository
{
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken);

    Task<EmailTemplate?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken);

    Task<List<EmailTemplate>> SearchAsync(
        Guid organizationId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken);
}
