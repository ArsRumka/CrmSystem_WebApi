using Deals.Application.Contracts;
using Deals.Application.Returns;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/deals")]
public sealed class DealReturnsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DealReturnsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Deals", PermissionAction.Read)]
    [HttpGet("{dealId:guid}/returns")]
    public async Task<IActionResult> GetDealReturns(Guid dealId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetDealReturnsQuery(dealId), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Read)]
    [HttpGet("returns/{id:guid}")]
    public async Task<IActionResult> GetDealReturnById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetDealReturnByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPost("{dealId:guid}/returns")]
    public async Task<IActionResult> CreateDealReturn(
        Guid dealId,
        [FromBody] CreateDealReturnBody body,
        CancellationToken cancellationToken)
    {
        var command = new CreateDealReturnCommand(dealId, body.Reason, body.Items);
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPut("returns/{id:guid}")]
    public async Task<IActionResult> UpdateDealReturn(
        Guid id,
        [FromBody] UpdateDealReturnBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDealReturnCommand(id, body.Reason, body.Items);
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPost("returns/{id:guid}/complete")]
    public async Task<IActionResult> CompleteDealReturn(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new CompleteDealReturnCommand(id), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPost("returns/{id:guid}/cancel")]
    public async Task<IActionResult> CancelDealReturn(
        Guid id,
        [FromBody] CancelDealReturnBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new CancelDealReturnCommand(id, body.CancellationReason),
            cancellationToken));
    }

    public sealed record CreateDealReturnBody(
        string Reason,
        IReadOnlyList<DealReturnItemRequest> Items);

    public sealed record UpdateDealReturnBody(
        string Reason,
        IReadOnlyList<DealReturnItemRequest> Items);

    public sealed record CancelDealReturnBody(string CancellationReason);
}
