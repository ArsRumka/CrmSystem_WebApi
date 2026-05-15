using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using Bonus.Application.Abstractions.Services;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Lookups;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Abstractions.Services;
using Deals.Application.Common;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record CreateDealCommand(
    Guid ClientId,
    Guid ResponsibleUserId,
    decimal BonusPointsUsed,
    string? Notes,
    IReadOnlyList<DealItemRequest> Items) : IRequest<DealResponse>;

public sealed class CreateDealCommandValidator : AbstractValidator<CreateDealCommand>
{
    public CreateDealCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.ResponsibleUserId).NotEmpty();
        RuleFor(x => x.BonusPointsUsed).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new DealItemRequestValidator());
    }
}

public sealed class CreateDealCommandHandler : IRequestHandler<CreateDealCommand, DealResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageInitializer _dealStageInitializer;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageHistoryRepository _dealStageHistoryRepository;
    private readonly IClientLookupService _clientLookupService;
    private readonly IUserLookupService _userLookupService;
    private readonly ICatalogLookupService _catalogLookupService;
    private readonly IBonusDealDiscountService _bonusDealDiscountService;
    private readonly DealCalculationService _calculationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDealCommandHandler(
        ICurrentUserService currentUserService,
        IDealStageInitializer dealStageInitializer,
        IDealStageRepository dealStageRepository,
        IDealRepository dealRepository,
        IDealStageHistoryRepository dealStageHistoryRepository,
        IClientLookupService clientLookupService,
        IUserLookupService userLookupService,
        ICatalogLookupService catalogLookupService,
        IBonusDealDiscountService bonusDealDiscountService,
        DealCalculationService calculationService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealStageInitializer = dealStageInitializer;
        _dealStageRepository = dealStageRepository;
        _dealRepository = dealRepository;
        _dealStageHistoryRepository = dealStageHistoryRepository;
        _clientLookupService = clientLookupService;
        _userLookupService = userLookupService;
        _catalogLookupService = catalogLookupService;
        _bonusDealDiscountService = bonusDealDiscountService;
        _calculationService = calculationService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealResponse> Handle(CreateDealCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, currentUserId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        await _dealStageInitializer.EnsureDefaultStagesAsync(organizationId, cancellationToken);

        var initialStage = await _dealStageRepository.GetInitialStageAsync(organizationId, cancellationToken)
            ?? throw new ConflictException("Initial deal stage was not found");

        if (!await _clientLookupService.ExistsActiveAsync(organizationId, request.ClientId, cancellationToken))
        {
            throw new NotFoundException("Client was not found");
        }

        if (!await _userLookupService.ExistsActiveAsync(organizationId, request.ResponsibleUserId, cancellationToken))
        {
            throw new NotFoundException("Responsible user was not found");
        }

        var dealId = Guid.NewGuid();
        var builtItems = await DealItemFactory.BuildAsync(
            organizationId,
            dealId,
            request.Items,
            _catalogLookupService,
            _calculationService,
            cancellationToken);

        var preBonusCalculation = _calculationService.CalculateDeal(
            builtItems.Calculations,
            bonusPointsUsed: 0,
            bonusDiscountAmount: 0);

        var bonusDiscount = await _bonusDealDiscountService.CalculateAsync(
            organizationId,
            request.ClientId,
            preBonusCalculation.FinalAmount,
            request.BonusPointsUsed,
            cancellationToken);

        var dealCalculation = _calculationService.CalculateDeal(
            builtItems.Calculations,
            bonusDiscount.AppliedBonusPoints,
            bonusDiscount.BonusDiscountAmount);

        var now = _dateTimeProvider.UtcNow;
        var deal = new Deal(
            dealId,
            organizationId,
            request.ClientId,
            request.ResponsibleUserId,
            initialStage.Id,
            dealCalculation.TotalAmount,
            dealCalculation.DiscountAmount,
            dealCalculation.BonusPointsUsed,
            dealCalculation.BonusDiscountAmount,
            dealCalculation.FinalAmount,
            request.Notes,
            now);

        deal.ReplaceItems(builtItems.Items);

        var history = new DealStageHistory(
            Guid.NewGuid(),
            organizationId,
            deal.Id,
            oldStageId: null,
            initialStage.Id,
            currentUserId,
            now);

        await _dealRepository.AddAsync(deal, cancellationToken);
        await _dealStageHistoryRepository.AddAsync(history, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Deals",
            AuditAction.Create,
            "Deal",
            deal.Id,
            $"Deal {deal.Id} was created",
            oldValues: null,
            newValues: DealAuditSnapshots.Deal(deal),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return deal.ToResponse(initialStage.Name, [history]);
    }
}
