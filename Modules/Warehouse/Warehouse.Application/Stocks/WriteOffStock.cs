using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Warehouse.Application.Abstractions.Repositories;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Application.Common;
using Warehouse.Application.Contracts;
using Warehouse.Domain.Enums;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Stocks;

public sealed record WriteOffStockCommand(
    Guid StorageId,
    Guid ProductId,
    decimal Quantity,
    string? Reason) : IRequest<ProductStockResponse>;

public sealed class WriteOffStockCommandValidator : AbstractValidator<WriteOffStockCommand>
{
    public WriteOffStockCommandValidator()
    {
        RuleFor(x => x.StorageId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class WriteOffStockCommandHandler : IRequestHandler<WriteOffStockCommand, ProductStockResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IWarehouseProductLookupService _productLookupService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public WriteOffStockCommandHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository,
        IProductStockRepository productStockRepository,
        IStockMovementRepository stockMovementRepository,
        IWarehouseProductLookupService productLookupService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
        _productStockRepository = productStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _productLookupService = productLookupService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductStockResponse> Handle(WriteOffStockCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await GetActiveStorageAsync(organizationId, request.StorageId, cancellationToken);

        if (!await _productLookupService.ExistsAsync(organizationId, request.ProductId, cancellationToken))
        {
            throw new NotFoundException("Product was not found");
        }

        var stock = await _productStockRepository.GetByStorageAndProductAsync(
            organizationId,
            request.StorageId,
            request.ProductId,
            cancellationToken);

        if (stock is null || stock.Quantity < request.Quantity)
        {
            throw new ConflictException("Insufficient stock quantity");
        }

        var now = _dateTimeProvider.UtcNow;
        var quantityBefore = stock.Quantity;
        stock.Decrease(request.Quantity, now);

        await _stockMovementRepository.AddAsync(
            new StockMovement(
                Guid.NewGuid(),
                organizationId,
                request.StorageId,
                request.ProductId,
                dealId: null,
                StockMovementType.WriteOff,
                request.Quantity,
                quantityBefore,
                stock.Quantity,
                request.Reason,
                now,
                userId),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock.ToResponse(storage.Name);
    }

    private async Task<Storage> GetActiveStorageAsync(
        Guid organizationId,
        Guid storageId,
        CancellationToken cancellationToken)
    {
        var storage = await _storageRepository.GetByIdAsync(organizationId, storageId, cancellationToken)
            ?? throw new NotFoundException("Storage was not found");

        if (!storage.IsActive)
        {
            throw new ConflictException("Storage is inactive");
        }

        return storage;
    }
}

