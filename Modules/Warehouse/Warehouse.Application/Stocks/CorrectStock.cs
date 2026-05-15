using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
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
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Application.Stocks;

public sealed record CorrectStockCommand(
    Guid StorageId,
    Guid ProductId,
    decimal NewQuantity,
    string? Reason) : IRequest<ProductStockResponse>;

public sealed class CorrectStockCommandValidator : AbstractValidator<CorrectStockCommand>
{
    public CorrectStockCommandValidator()
    {
        RuleFor(x => x.StorageId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class CorrectStockCommandHandler : IRequestHandler<CorrectStockCommand, ProductStockResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IStorageRepository _storageRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IWarehouseProductLookupService _productLookupService;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CorrectStockCommandHandler(
        ICurrentUserService currentUserService,
        IStorageRepository storageRepository,
        IProductStockRepository productStockRepository,
        IStockMovementRepository stockMovementRepository,
        IWarehouseProductLookupService productLookupService,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _storageRepository = storageRepository;
        _productStockRepository = productStockRepository;
        _stockMovementRepository = stockMovementRepository;
        _productLookupService = productLookupService;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductStockResponse> Handle(CorrectStockCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = WarehouseApplicationGuards.RequireOrganizationUser(_currentUserService);

        var storage = await GetActiveStorageAsync(organizationId, request.StorageId, cancellationToken);

        if (!await _productLookupService.ExistsAsync(organizationId, request.ProductId, cancellationToken))
        {
            throw new NotFoundException("Product was not found");
        }

        var now = _dateTimeProvider.UtcNow;
        var stock = await _productStockRepository.GetByStorageAndProductAsync(
            organizationId,
            request.StorageId,
            request.ProductId,
            cancellationToken);

        if (stock is null)
        {
            stock = new ProductStock(
                Guid.NewGuid(),
                organizationId,
                request.StorageId,
                request.ProductId,
                quantity: 0,
                now);

            await _productStockRepository.AddAsync(stock, cancellationToken);
        }

        var quantityBefore = stock.Quantity;

        if (quantityBefore != request.NewQuantity)
        {
            stock.Correct(request.NewQuantity, now);
            var movementQuantity = Math.Abs(request.NewQuantity - quantityBefore);

            var movement = new StockMovement(
                Guid.NewGuid(),
                organizationId,
                request.StorageId,
                request.ProductId,
                dealId: null,
                StockMovementType.Correction,
                movementQuantity,
                quantityBefore,
                stock.Quantity,
                request.Reason,
                now,
                userId);

            await _stockMovementRepository.AddAsync(movement, cancellationToken);
            await _auditLogService.LogAsync(
                organizationId,
                userId,
                "Warehouse",
                AuditAction.ManualAdjustment,
                "StockMovement",
                movement.Id,
                $"Stock correction for product {request.ProductId}",
                oldValues: WarehouseAuditSnapshots.StockMovement(
                    request.StorageId,
                    request.ProductId,
                    movementQuantity,
                    quantityBefore,
                    quantityBefore,
                    StockMovementType.Correction.ToString()),
                newValues: WarehouseAuditSnapshots.StockMovement(
                    request.StorageId,
                    request.ProductId,
                    movementQuantity,
                    quantityBefore,
                    stock.Quantity,
                    StockMovementType.Correction.ToString()),
                cancellationToken);
        }

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
