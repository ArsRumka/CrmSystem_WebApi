using Identity.Domain.Entities;

namespace Identity.Application.Abstractions.Repositories;

public interface IEmailConfirmationTokenRepository
{
    Task AddAsync(EmailConfirmationToken token, CancellationToken cancellationToken = default);

    Task<EmailConfirmationToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
