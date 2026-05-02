using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly AppDbContext _dbContext;

    public EmailConfirmationTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<EmailConfirmationToken>().AddAsync(token, cancellationToken);
    }

    public Task<EmailConfirmationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<EmailConfirmationToken>()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}
