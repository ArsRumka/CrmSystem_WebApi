using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using Bonus.Application.Abstractions.Services;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using Deals.Domain.Enums;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Services;

namespace Deals.Application.Returns;

public sealed record CompleteDealReturnCommand(Guid Id) : IRequest<DealReturnResponse>;

public sealed class CompleteDealReturnCommandValidator : AbstractValidator<CompleteDealReturnCommand>
{
    public CompleteDealReturnCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class CompleteDealReturnCommandHandler : IRequestHandler<CompleteDealReturnCommand, DealReturnResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealReturnRepository _dealReturnRepository;
    private readonly IWarehouseDealReturnService _warehouseDealReturnService;
    private readonly IBonusDealReturnService _bonusDealReturnService;
    private readonly DealReturnCalculationService _calculationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteDealReturnCommandHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository,
        IDealReturnRepository dealReturnRepository,
        IWarehouseDealReturnService warehouseDealReturnService,
        IBonusDealReturnService bonusDealReturnService,
        DealReturnCalculationService calculationService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
        _dealReturnRepository = dealReturnRepository;
        _warehouseDealReturnService = warehouseDealReturnService;
        _bonusDealReturnService = bonusDealReturnService;
        _calculationService = calculationService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealReturnResponse> Handle(
        CompleteDealReturnCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var dealReturn = await _dealReturnRepository.GetByIdWithItemsAsync(
            organizationId,
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Deal return was not found");

        DealReturnGuards.EnsureDraft(dealReturn);

        var deal = await _dealRepository.GetByIdWithItemsAsync(organizationId, dealReturn.DealId, cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var stage = await _dealStageRepository.GetByIdAsync(organizationId, deal.StageId, cancellationToken)
            ?? throw new ConflictException("Current deal stage was not found");

        DealReturnGuards.EnsureReturnableDeal(deal, stage);

        var completedItems = await _dealReturnRepository.GetCompletedItemsByDealIdAsync(
            organizationId,
            deal.Id,
            cancellationToken);

        var completedReturns = await _dealReturnRepository.GetCompletedByDealIdAsync(
            organizationId,
            deal.Id,
            cancellationToken);

        var itemRequests = dealReturn.Items
            .Select(item => new DealReturnItemRequest(item.DealItemId, item.Quantity, item.StorageId))
            .ToList();

        var calculation = _calculationService.Calculate(
            deal,
            dealReturn.Id,
            itemRequests,
            completedItems,
            completedReturns);

        var warehouseItems = calculation.Items
            .Where(item => item.ItemType == DealItemType.Product)
            .Select(item => new WarehouseDealReturnItem(
                item.DealItemId,
                item.ItemId,
                item.StorageId!.Value,
                item.Quantity))
            .ToList();

        await _warehouseDealReturnService.ProcessReturnAsync(
            organizationId,
            deal.Id,
            dealReturn.Id,
            userId,
            warehouseItems,
            dealReturn.Reason,
            cancellationToken);

        var bonusResult = await _bonusDealReturnService.ProcessReturnAsync(
            organizationId,
            deal.Id,
            dealReturn.Id,
            userId,
            calculation.ReturnRatio,
            calculation.TotalAmount,
            calculation.BonusDiscountMoneyShare,
            dealReturn.Reason,
            cancellationToken);

        var oldSnapshot = DealAuditSnapshots.DealReturn(dealReturn);

        dealReturn.Complete(
            calculation.TotalAmount,
            bonusResult.BonusPointsReturned,
            bonusResult.BonusAccrualReversed,
            calculation.MoneyAmount,
            userId,
            _dateTimeProvider.UtcNow);

        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Deals",
            AuditAction.Complete,
            "DealReturn",
            dealReturn.Id,
            $"Deal return {dealReturn.Id} was completed",
            oldSnapshot,
            newValues: DealAuditSnapshots.DealReturn(dealReturn),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dealReturn.ToResponse();
    }
}
