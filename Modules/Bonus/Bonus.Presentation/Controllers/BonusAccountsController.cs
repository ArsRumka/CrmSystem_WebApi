using Bonus.Application.Accounts;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bonus.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/bonus/accounts")]
public sealed class BonusAccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BonusAccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetBonusAccounts(
        [FromQuery] Guid? clientId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetBonusAccountsQuery(clientId, isActive), cancellationToken));
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBonusAccountById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetBonusAccountByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet("by-client/{clientId:guid}")]
    public async Task<IActionResult> GetBonusAccountByClientId(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetBonusAccountByClientIdQuery(clientId), cancellationToken));
    }

    [RequirePermission("Bonus", PermissionAction.Update)]
    [HttpPost("by-client/{clientId:guid}/adjust")]
    public async Task<IActionResult> AdjustBonusAccount(
        Guid clientId,
        [FromBody] AdjustBonusAccountBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new AdjustBonusAccountCommand(clientId, body.PointsDelta, body.Reason),
            cancellationToken));
    }

    public sealed record AdjustBonusAccountBody(decimal PointsDelta, string Reason);
}
