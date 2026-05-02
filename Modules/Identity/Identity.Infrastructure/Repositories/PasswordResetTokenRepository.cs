using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application.Abstractions.Repositories;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppDbContext _dbContext;

    public PasswordResetTokenRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<PasswordResetToken>().AddAsync(token, cancellationToken);
    }

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<PasswordResetToken>()
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }
}
