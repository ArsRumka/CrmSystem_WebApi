using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Movements;
using Warehouse.Domain.Enums;

namespace Warehouse.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/warehouse/movements")]
public sealed class StockMovementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockMovementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? storageId,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? dealId,
        [FromQuery] StockMovementType? type,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetStockMovementsQuery(storageId, productId, dealId, type, dateFrom, dateTo),
            cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStockMovementById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetStockMovementByIdQuery(id), cancellationToken));
    }
}

