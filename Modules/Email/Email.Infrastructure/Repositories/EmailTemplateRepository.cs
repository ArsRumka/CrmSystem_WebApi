using Email.Application.Abstractions.Repositories;
using Email.Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Email.Infrastructure.Repositories;

public sealed class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailTemplateRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailTemplate template, CancellationToken cancellationToken)
    {
        await _dbContext.Set<EmailTemplate>().AddAsync(template, cancellationToken);
    }

    public Task<EmailTemplate?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Set<EmailTemplate>()
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == id, cancellationToken);
    }

    public async Task<List<EmailTemplate>> SearchAsync(
        Guid organizationId,
        bool? isActive,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<EmailTemplate>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, pattern) ||
                EF.Functions.ILike(x.Subject, pattern));
        }

        return await query
            .OrderBy(x => x.Name)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
