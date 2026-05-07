using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using Bonus.Domain.Entities;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using MediatR;

namespace Bonus.Application.Settings;

public sealed record GetBonusSettingsQuery : IRequest<BonusSettingsResponse>;

public sealed class GetBonusSettingsQueryHandler : IRequestHandler<GetBonusSettingsQuery, BonusSettingsResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusSettingsRepository _settingsRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public GetBonusSettingsQueryHandler(
        ICurrentUserService currentUserService,
        IBonusSettingsRepository settingsRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _settingsRepository = settingsRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<BonusSettingsResponse> Handle(GetBonusSettingsQuery request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        if (settings is null)
        {
            settings = BonusSettings.CreateDefault(organizationId, _dateTimeProvider.UtcNow);
            await _settingsRepository.AddAsync(settings, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return settings.ToResponse();
    }
}
