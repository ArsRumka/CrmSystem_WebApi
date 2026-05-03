using BuildingBlocks.Application.Abstractions.Auth;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Movements;

public sealed record GetStockMovementsQuery(
    Guid? StorageId,
    Guid? ProductId,
    Guid? DealId,
    StockMovementType? Type,
    DateTime? DateFrom,
    DateTime? DateTo) : IRequest<IReadOnlyList<StockMovementResponse>>;

public sealed class GetStockMovementsQueryValidator : AbstractValidator<GetStockMovementsQuery>
{
    public GetStockMovementsQueryValidator()
    {
        RuleFor(x => x.StorageId).NotEqual(Guid.Empty).When(x => x.StorageId.HasValue);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
        RuleFor(x => x.DealId).NotEqual(Guid.Empty).When(x => x.DealId.HasValue);
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom.Value <= x.DateTo.Value)
            .WithMessage("DateFrom must be less than or equal to DateTo");
    }
}

public sealed class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageRepository _storageRepository;

    public GetStockMovementsQueryHandler(
        ICurrentUserService currentUserService,
        IStockMovementRepository stockMovementRepository,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _stockMovementRepository = stockMovementRepository;
        _storageRepository = storageRepository;
    }

    public async Task<IReadOnlyList<StockMovementResponse>> Handle(
        GetStockMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var movements = await _stockMovementRepository.SearchAsync(
            organizationId,
            request.StorageId,
            request.ProductId,
            request.DealId,
            request.Type,
            request.DateFrom,
            request.DateTo,
            cancellationToken);

        var storageNames = await GetStorageNamesAsync(organizationId, cancellationToken);

        return movements
            .Select(movement => movement.ToResponse(storageNames.GetValueOrDefault(movement.StorageId, string.Empty)))
            .ToList();
    }

    private async Task<Dictionary<Guid, string>> GetStorageNamesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var storages = await _storageRepository.SearchAsync(organizationId, null, null, cancellationToken);
        return storages.ToDictionary(storage => storage.Id, storage => storage.Name);
    }
}

