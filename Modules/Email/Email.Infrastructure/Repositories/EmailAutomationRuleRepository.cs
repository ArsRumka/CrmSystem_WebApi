using Email.Application.Abstractions.Repositories;
using Email.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Repositories;

public sealed class EmailAutomationRuleRepository : IEmailAutomationRuleRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailAutomationRuleRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailAutomationRule rule, CancellationToken cancellationToken)
    {
        await _dbContext.Set<EmailAutomationRule>().AddAsync(rule, cancellationToken);
    }

    public Task<EmailAutomationRule?> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailAutomationRule>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<List<EmailAutomationRule>> GetEnabledRulesAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Set<EmailAutomationRule>()
            .Where(x => x.IsEnabled)
            .ToListAsync(cancellationToken);
    }
}
