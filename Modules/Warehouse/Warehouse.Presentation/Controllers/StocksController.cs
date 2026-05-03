using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.Stocks;

namespace Warehouse.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/warehouse/stocks")]
public sealed class StocksController : ControllerBase
{
    private readonly IMediator _mediator;

    public StocksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetStocks(
        [FromQuery] Guid? storageId,
        [FromQuery] Guid? productId,
        [FromQuery] bool? onlyPositive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetStocksQuery(storageId, productId, onlyPositive ?? false),
            cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStockById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetStockByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Create)]
    [HttpPost("receipt")]
    public async Task<IActionResult> ReceiptStock(
        [FromBody] ReceiptStockCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Update)]
    [HttpPost("write-off")]
    public async Task<IActionResult> WriteOffStock(
        [FromBody] WriteOffStockCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Warehouse", PermissionAction.Update)]
    [HttpPost("correction")]
    public async Task<IActionResult> CorrectStock(
        [FromBody] CorrectStockCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }
}

