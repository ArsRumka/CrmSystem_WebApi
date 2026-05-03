using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Stocks;

public sealed record GetStockByIdQuery(Guid Id) : IRequest<ProductStockResponse>;

public sealed class GetStockByIdQueryValidator : AbstractValidator<GetStockByIdQuery>
{
    public GetStockByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetStockByIdQueryHandler : IRequestHandler<GetStockByIdQuery, ProductStockResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStorageRepository _storageRepository;

    public GetStockByIdQueryHandler(
        ICurrentUserService currentUserService,
        IProductStockRepository productStockRepository,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _productStockRepository = productStockRepository;
        _storageRepository = storageRepository;
    }

    public async Task<ProductStockResponse> Handle(GetStockByIdQuery request, CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var stock = await _productStockRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Product stock was not found");

        var storage = await _storageRepository.GetByIdAsync(organizationId, stock.StorageId, cancellationToken);

        return stock.ToResponse(storage?.Name ?? string.Empty);
    }
}

