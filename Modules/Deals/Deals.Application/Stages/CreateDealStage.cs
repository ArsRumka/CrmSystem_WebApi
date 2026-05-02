using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using Deals.Application.Abstractions.Repositories;
using Deals.Application.Abstractions.Services;
using Deals.Application.Common;
using Deals.Application.Contracts;
using Deals.Domain.Entities;
using FluentValidation;
using MediatR;

namespace Deals.Application.Stages;

public sealed record CreateDealStageCommand(
    string Name,
    int Order,
    bool IsFinal,
    bool IsSuccessful) : IRequest<DealStageResponse>;

public sealed class CreateDealStageCommandValidator : AbstractValidator<CreateDealStageCommand>
{
    public CreateDealStageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}

public sealed class CreateDealStageCommandHandler : IRequestHandler<CreateDealStageCommand, DealStageResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDealStageInitializer _dealStageInitializer;
    private readonly IDealStageRepository _dealStageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDealStageCommandHandler(
        ICurrentUserService currentUserService,
        IDealStageInitializer dealStageInitializer,
        IDealStageRepository dealStageRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _dealStageInitializer = dealStageInitializer;
        _dealStageRepository = dealStageRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<DealStageResponse> Handle(
        CreateDealStageCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = DealsApplicationGuards.RequireOrganizationUser(_currentUserService);

        await _dealStageInitializer.EnsureDefaultStagesAsync(organizationId, cancellationToken);

        var stage = new DealStage(
            Guid.NewGuid(),
            organizationId,
            request.Name,
            request.Order,
            request.IsFinal,
            request.IsSuccessful,
            _dateTimeProvider.UtcNow);

        await _dealStageRepository.AddAsync(stage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stage.ToResponse();
    }
}
