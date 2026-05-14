using Email.Application.Abstractions.Repositories;
using Email.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Repositories;

public sealed class EmailSettingsRepository : IEmailSettingsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailSettingsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailSettings settings, CancellationToken cancellationToken)
    {
        await _dbContext.Set<EmailSettings>().AddAsync(settings, cancellationToken);
    }

    public Task<EmailSettings?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailSettings>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }
}
