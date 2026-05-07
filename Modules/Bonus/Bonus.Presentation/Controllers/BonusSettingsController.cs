using Bonus.Application.Settings;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bonus.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/bonus/settings")]
public sealed class BonusSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BonusSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Bonus", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetBonusSettings(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetBonusSettingsQuery(), cancellationToken));
    }

    [RequirePermission("Bonus", PermissionAction.Update)]
    [HttpPut]
    public async Task<IActionResult> UpdateBonusSettings(
        [FromBody] UpdateBonusSettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }
}
