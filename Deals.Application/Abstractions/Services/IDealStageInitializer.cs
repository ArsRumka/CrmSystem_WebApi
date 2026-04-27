namespace Deals.Application.Abstractions.Services;

public interface IDealStageInitializer
{
    Task EnsureDefaultStagesAsync(Guid organizationId, CancellationToken cancellationToken);
}
