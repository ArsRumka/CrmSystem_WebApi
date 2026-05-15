using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Deals.Application.Returns;

public sealed record UpdateDealReturnCommand(
    Guid Id,
    string Reason,
    IReadOnlyList<DealReturnItemRequest> Items) : IRequest<DealReturnResponse>;

public sealed class UpdateDealReturnCommandValidator : AbstractValidator<UpdateDealReturnCommand>
{
    public UpdateDealReturnCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new DealReturnItemRequestValidator());
    }
}

public sealed class UpdateDealReturnCommandHandler : IRequestHandler<UpdateDealReturnCommand, DealReturnResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealReturnRepository _dealReturnRepository;
    private readonly DealReturnCalculationService _calculationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDealReturnCommandHandler(
        ICurrentUserService currentUserService,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository,
        IDealReturnRepository dealReturnRepository,
        DealReturnCalculationService calculationService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
        _dealReturnRepository = dealReturnRepository;
        _calculationService = calculationService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealReturnResponse> Handle(
        UpdateDealReturnCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, currentUserId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

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

        var calculation = _calculationService.Calculate(
            deal,
            dealReturn.Id,
            request.Items,
            completedItems,
            completedReturns);

        var oldSnapshot = DealAuditSnapshots.DealReturn(dealReturn);

        dealReturn.Update(
            request.Reason,
            calculation.TotalAmount,
            calculation.MoneyAmount,
            _dateTimeProvider.UtcNow,
            calculation.Items);

        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Deals",
            AuditAction.Update,
            "DealReturn",
            dealReturn.Id,
            $"Deal return {dealReturn.Id} was updated",
            oldSnapshot,
            DealAuditSnapshots.DealReturn(dealReturn),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dealReturn.ToResponse();
    }
}
