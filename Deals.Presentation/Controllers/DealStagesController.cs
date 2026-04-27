using Deals.Application.Stages;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Deals.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/deals/stages")]
public sealed class DealStagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DealStagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Deals", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetDealStages(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetDealStagesQuery(search, isActive), cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateDealStage(
        [FromBody] CreateDealStageCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDealStage(
        Guid id,
        [FromBody] UpdateDealStageBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDealStageCommand(
            id,
            body.Name,
            body.Order,
            body.IsFinal,
            body.IsSuccessful);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Deals", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateDealStage(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateDealStageCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateDealStageBody(
        string Name,
        int Order,
        bool IsFinal,
        bool IsSuccessful);
}
