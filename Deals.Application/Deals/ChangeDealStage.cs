using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Abstractions.Services;
using Deals.Application.Common;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Deals.Application.Deals;

public sealed record ChangeDealStageCommand(Guid Id, Guid StageId) : IRequest<DealResponse>;

public sealed class ChangeDealStageCommandValidator : AbstractValidator<ChangeDealStageCommand>
{
    public ChangeDealStageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StageId).NotEmpty();
    }
}

public sealed class ChangeDealStageCommandHandler : IRequestHandler<ChangeDealStageCommand, DealResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageInitializer _dealStageInitializer;
    private readonly IDealRepository _dealRepository;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDealStageHistoryRepository _dealStageHistoryRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDealStageCommandHandler(
        ICurrentUserService currentUserService,
        IDealStageInitializer dealStageInitializer,
        IDealRepository dealRepository,
        IDealStageRepository dealStageRepository,
        IDealStageHistoryRepository dealStageHistoryRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealStageInitializer = dealStageInitializer;
        _dealRepository = dealRepository;
        _dealStageRepository = dealStageRepository;
        _dealStageHistoryRepository = dealStageHistoryRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealResponse> Handle(
        ChangeDealStageCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, currentUserId) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        await _dealStageInitializer.EnsureDefaultStagesAsync(organizationId, cancellationToken);

        var deal = await _dealRepository.GetByIdWithItemsAndHistoryAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var currentStage = await _dealStageRepository.GetByIdAsync(organizationId, deal.StageId, cancellationToken)
            ?? throw new ConflictException("Current deal stage was not found");

        if (currentStage.IsFinal)
        {
            throw new ConflictException("Final-stage deals cannot change stage");
        }

        if (deal.StageId == request.StageId)
        {
            throw new ConflictException("Deal already has requested stage");
        }

        var newStage = await _dealStageRepository.GetByIdAsync(organizationId, request.StageId, cancellationToken)
            ?? throw new NotFoundException("Deal stage was not found");

        if (!newStage.IsActive)
        {
            throw new ConflictException("Deal stage is inactive");
        }

        var now = _dateTimeProvider.UtcNow;
        var oldStageId = deal.StageId;

        deal.ChangeStage(newStage.Id, newStage.IsFinal, now);

        var history = new DealStageHistory(
            Guid.NewGuid(),
            organizationId,
            deal.Id,
            oldStageId,
            newStage.Id,
            currentUserId,
            now);

        await _dealStageHistoryRepository.AddAsync(history, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var stageHistory = deal.StageHistory.Concat([history]).ToList();

        return deal.ToResponse(newStage.Name, stageHistory);
    }
}
