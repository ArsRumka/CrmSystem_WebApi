using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Common;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Deals.Application.Returns;

public sealed record CreateDealReturnCommand(
    Guid DealId,
    string Reason,
    IReadOnlyList<DealReturnItemRequest> Items) : IRequest<DealReturnResponse>;

public sealed class CreateDealReturnCommandValidator : AbstractValidator<CreateDealReturnCommand>
{
    public CreateDealReturnCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new DealReturnItemRequestValidator());
    }
}

public sealed class CreateDealReturnCommandHandler : IRequestHandler<CreateDealReturnCommand, DealReturnResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealReturnRepository _dealReturnRepository;
    private readonly DealReturnCalculationService _calculationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDealReturnCommandHandler(
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
        CreateDealReturnCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var deal = await _dealRepository.GetByIdWithItemsAsync(organizationId, request.DealId, cancellationToken)
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

        var dealReturnId = Guid.NewGuid();
        var calculation = _calculationService.Calculate(
            deal,
            dealReturnId,
            request.Items,
            completedItems,
            completedReturns);

        var dealReturn = new DealReturn(
            dealReturnId,
            organizationId,
            deal.Id,
            request.Reason,
            calculation.TotalAmount,
            calculation.MoneyAmount,
            userId,
            _dateTimeProvider.UtcNow,
            calculation.Items);

        await _dealReturnRepository.AddAsync(dealReturn, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            userId,
            "Deals",
            AuditAction.Return,
            "DealReturn",
            dealReturn.Id,
            $"Deal return {dealReturn.Id} was created",
            oldValues: null,
            newValues: DealAuditSnapshots.DealReturn(dealReturn),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return dealReturn.ToResponse();
    }
}
