using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Abstractions.Services;
using Deals.Domain.Entities;

namespace Deals.Application.Common;

public sealed class DealStageInitializer : IDealStageInitializer
{
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DealStageInitializer(
        IDealStageRepository dealStageRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _dealStageRepository = dealStageRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task EnsureDefaultStagesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (await _dealStageRepository.AnyAsync(organizationId, cancellationToken))
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        var stages = new[]
        {
            new DealStage(Guid.NewGuid(), organizationId, "New", 1, isFinal: false, isSuccessful: false, now),
            new DealStage(Guid.NewGuid(), organizationId, "InProgress", 2, isFinal: false, isSuccessful: false, now),
            new DealStage(Guid.NewGuid(), organizationId, "Completed", 3, isFinal: true, isSuccessful: true, now),
            new DealStage(Guid.NewGuid(), organizationId, "Cancelled", 4, isFinal: true, isSuccessful: false, now)
        };

        await _dealStageRepository.AddRangeAsync(stages, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
