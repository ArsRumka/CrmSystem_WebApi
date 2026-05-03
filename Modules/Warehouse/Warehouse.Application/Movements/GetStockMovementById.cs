using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Movements;

public sealed record GetStockMovementByIdQuery(Guid Id) : IRequest<StockMovementResponse>;

public sealed class GetStockMovementByIdQueryValidator : AbstractValidator<GetStockMovementByIdQuery>
{
    public GetStockMovementByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetStockMovementByIdQueryHandler
    : IRequestHandler<GetStockMovementByIdQuery, StockMovementResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStorageRepository _storageRepository;

    public GetStockMovementByIdQueryHandler(
        ICurrentUserService currentUserService,
        IStockMovementRepository stockMovementRepository,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _stockMovementRepository = stockMovementRepository;
        _storageRepository = storageRepository;
    }

    public async Task<StockMovementResponse> Handle(
        GetStockMovementByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var movement = await _stockMovementRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Stock movement was not found");

        var storage = await _storageRepository.GetByIdAsync(organizationId, movement.StorageId, cancellationToken);

        return movement.ToResponse(storage?.Name ?? string.Empty);
    }
}

