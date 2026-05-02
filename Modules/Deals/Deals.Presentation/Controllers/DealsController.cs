using Deals.Application.Contracts;
using Deals.Application.Deals;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/deals")]
public sealed class DealsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DealsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Deals", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetDeals(
        [FromQuery] string? search,
        [FromQuery] Guid? clientId,
        [FromQuery] Guid? responsibleUserId,
        [FromQuery] Guid? stageId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = new GetDealsQuery(
            search,
            clientId,
            responsibleUserId,
            stageId,
            dateFrom,
            dateTo,
            isActive);

        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDealById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetDealByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateDeal(
        [FromBody] CreateDealCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDeal(
        Guid id,
        [FromBody] UpdateDealBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDealCommand(
            id,
            body.ClientId,
            body.ResponsibleUserId,
            body.BonusPointsUsed,
            body.Notes,
            body.Items);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPut("{id:guid}/stage")]
    public async Task<IActionResult> ChangeDealStage(
        Guid id,
        [FromBody] ChangeDealStageBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ChangeDealStageCommand(id, body.StageId), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateDeal(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateDealCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateDealBody(
        Guid ClientId,
        Guid ResponsibleUserId,
        decimal BonusPointsUsed,
        string? Notes,
        IReadOnlyList<DealItemRequest> Items);

    public sealed record ChangeDealStageBody(Guid StageId);
}
