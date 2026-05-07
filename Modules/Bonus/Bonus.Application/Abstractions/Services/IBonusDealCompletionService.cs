namespace Bonus.Application.Abstractions.Services;

public interface IBonusDealCompletionService
{
    Task CompleteDealAsync(
        Guid organizationId,
        Guid dealId,
        Guid userId,
        CancellationToken cancellationToken);
}
