using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Infrastructure.Services;

public sealed class WarehouseDealReturnService : IWarehouseDealReturnService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WarehouseDealReturnService(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task ProcessReturnAsync(
        Guid organizationId,
        Guid dealId,
        Guid dealReturnId,
        Guid userId,
        IReadOnlyCollection<WarehouseDealReturnItem> items,
        string reason,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return;
        }

        if (await _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.SourceReturnId == dealReturnId &&
                     x.Type == StockMovementType.Return,
                cancellationToken))
        {
            throw new ConflictException("Deal return stock was already processed");
        }

        var now = _dateTimeProvider.UtcNow;

        foreach (var item in items)
        {
            if (item.DealItemId == Guid.Empty)
                throw new ArgumentException("DealItemId is required", nameof(items));

            if (item.ProductId == Guid.Empty)
                throw new ArgumentException("ProductId is required", nameof(items));

            if (item.StorageId == Guid.Empty)
                throw new ArgumentException("StorageId is required", nameof(items));

            if (item.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero", nameof(items));

            var storage = await _dbContext.Set<Storage>()
                .FirstOrDefaultAsync(
                    x => x.OrganizationId == organizationId &&
                         x.Id == item.StorageId,
                    cancellationToken)
                ?? throw new NotFoundException("Storage was not found");

            if (!storage.IsActive)
            {
                throw new ConflictException("Storage is inactive");
            }

            var stock = await _dbContext.Set<ProductStock>()
                .FirstOrDefaultAsync(
                    x => x.OrganizationId == organizationId &&
                         x.StorageId == item.StorageId &&
                         x.ProductId == item.ProductId,
                    cancellationToken);

            if (stock is null)
            {
                stock = new ProductStock(
                    Guid.NewGuid(),
                    organizationId,
                    item.StorageId,
                    item.ProductId,
                    0,
                    now);

                await _dbContext.Set<ProductStock>().AddAsync(stock, cancellationToken);
            }

            var quantityBefore = stock.Quantity;
            stock.Increase(item.Quantity, now);

            await _dbContext.Set<StockMovement>().AddAsync(
                new StockMovement(
                    Guid.NewGuid(),
                    organizationId,
                    item.StorageId,
                    item.ProductId,
                    dealId,
                    StockMovementType.Return,
                    item.Quantity,
                    quantityBefore,
                    stock.Quantity,
                    reason,
                    now,
                    userId,
                    dealReturnId),
                cancellationToken);
        }
    }
}
