using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using Bonus.Application.Abstractions.Repositories;
using Bonus.Application.Common;
using Bonus.Application.Contracts;
using Bonus.Domain.Entities;
using Bonus.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using FluentValidation;
using MediatR;

namespace Bonus.Application.Settings;

public sealed record UpdateBonusSettingsCommand(
    bool IsEnabled,
    decimal PointValue,
    BonusAccrualType AccrualType,
    decimal AccrualValue,
    decimal MaxPaymentPercent,
    bool AccrueOnBonusPayment) : IRequest<BonusSettingsResponse>;

public sealed class UpdateBonusSettingsCommandValidator : AbstractValidator<UpdateBonusSettingsCommand>
{
    public UpdateBonusSettingsCommandValidator()
    {
        RuleFor(x => x.PointValue).GreaterThan(0);
        RuleFor(x => x.AccrualType).IsInEnum();
        RuleFor(x => x.AccrualValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.AccrualValue)
            .LessThanOrEqualTo(100)
            .When(x => x.AccrualType == BonusAccrualType.Percent);
        RuleFor(x => x.MaxPaymentPercent).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateBonusSettingsCommandHandler
    : IRequestHandler<UpdateBonusSettingsCommand, BonusSettingsResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IBonusSettingsRepository _settingsRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBonusSettingsCommandHandler(
        ICurrentUserService currentUserService,
        IBonusSettingsRepository settingsRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _settingsRepository = settingsRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<BonusSettingsResponse> Handle(
        UpdateBonusSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = BonusApplicationGuards.RequireOrganizationUser(_currentUserService);
        var now = _dateTimeProvider.UtcNow;

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);
        object? oldSnapshot = null;
        if (settings is null)
        {
            settings = BonusSettings.CreateDefault(organizationId, now);
            await _settingsRepository.AddAsync(settings, cancellationToken);
        }
        else
        {
            oldSnapshot = BonusAuditSnapshots.Settings(settings);
        }

        settings.Update(
            request.IsEnabled,
            request.PointValue,
            request.AccrualType,
            request.AccrualValue,
            request.MaxPaymentPercent,
            request.AccrueOnBonusPayment,
            now);

        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Bonus",
            AuditAction.Update,
            "BonusSettings",
            settings.Id,
            "Bonus settings were updated",
            oldSnapshot,
            BonusAuditSnapshots.Settings(settings),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return settings.ToResponse();
    }
}
