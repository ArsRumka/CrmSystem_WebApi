using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Deals.Domain.Entities;
using Deals.Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Abstractions.Services;
using Warehouse.Domain.Entities;
using Warehouse.Domain.Enums;

namespace Warehouse.Infrastructure.Services;

public sealed class WarehouseDealCompletionService : IWarehouseDealCompletionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public WarehouseDealCompletionService(
        ApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task CompleteDealAsync(
        Guid organizationId,
        Guid dealId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<StockMovement>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId &&
                     x.DealId == dealId &&
                     x.Type == StockMovementType.Sale,
                cancellationToken))
        {
            throw new ConflictException("Deal stock was already deducted");
        }

        var deal = await _dbContext.Set<Deal>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == dealId,
                cancellationToken)
            ?? throw new NotFoundException("Deal was not found");

        var productItems = deal.Items
            .Where(item => item.ItemType == DealItemType.Product)
            .ToList();

        if (productItems.Count == 0)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        foreach (var item in productItems)
        {
            if (!item.StorageId.HasValue)
            {
                throw new ConflictException("StorageId is required for product deal items");
            }

            var storage = await _dbContext.Set<Storage>()
                .FirstOrDefaultAsync(
                    x => x.OrganizationId == organizationId &&
                         x.Id == item.StorageId.Value,
                    cancellationToken)
                ?? throw new NotFoundException("Storage was not found");

            if (!storage.IsActive)
            {
                throw new ConflictException("Storage is inactive");
            }

            var stock = await _dbContext.Set<ProductStock>()
                .FirstOrDefaultAsync(
                    x => x.OrganizationId == organizationId &&
                         x.StorageId == item.StorageId.Value &&
                         x.ProductId == item.ItemId,
                    cancellationToken);

            if (stock is null || stock.Quantity < item.Quantity)
            {
                throw new ConflictException("Insufficient stock quantity");
            }

            var quantityBefore = stock.Quantity;
            stock.Decrease(item.Quantity, now);

            await _dbContext.Set<StockMovement>().AddAsync(
                new StockMovement(
                    Guid.NewGuid(),
                    organizationId,
                    item.StorageId.Value,
                    item.ItemId,
                    dealId,
                    StockMovementType.Sale,
                    item.Quantity,
                    quantityBefore,
                    stock.Quantity,
                    reason: null,
                    now,
                    userId),
                cancellationToken);
        }
    }
}

