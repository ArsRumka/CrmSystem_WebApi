using BuildingBlocks.Application.Abstractions.Auth;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;

namespace Warehouse.Application.Stocks;

public sealed record GetStocksQuery(
    Guid? StorageId,
    Guid? ProductId,
    bool OnlyPositive) : IRequest<IReadOnlyList<ProductStockResponse>>;

public sealed class GetStocksQueryValidator : AbstractValidator<GetStocksQuery>
{
    public GetStocksQueryValidator()
    {
        RuleFor(x => x.StorageId).NotEqual(Guid.Empty).When(x => x.StorageId.HasValue);
        RuleFor(x => x.ProductId).NotEqual(Guid.Empty).When(x => x.ProductId.HasValue);
    }
}

public sealed class GetStocksQueryHandler : IRequestHandler<GetStocksQuery, IReadOnlyList<ProductStockResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStorageRepository _storageRepository;

    public GetStocksQueryHandler(
        ICurrentUserService currentUserService,
        IProductStockRepository productStockRepository,
        IStorageRepository storageRepository)
    {
        _currentUserService = currentUserService;
        _productStockRepository = productStockRepository;
        _storageRepository = storageRepository;
    }

    public async Task<IReadOnlyList<ProductStockResponse>> Handle(
        GetStocksQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var stocks = await _productStockRepository.SearchAsync(
            organizationId,
            request.StorageId,
            request.ProductId,
            request.OnlyPositive,
            cancellationToken);

        var storageNames = await GetStorageNamesAsync(organizationId, cancellationToken);

        return stocks
            .Select(stock => stock.ToResponse(storageNames.GetValueOrDefault(stock.StorageId, string.Empty)))
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

