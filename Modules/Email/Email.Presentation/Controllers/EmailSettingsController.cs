using Email.Application.Settings;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Email.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/email/settings")]
public sealed class EmailSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetEmailSettings(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailSettingsQuery(), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Update)]
    [HttpPut]
    public async Task<IActionResult> UpdateEmailSettings(
        [FromBody] UpdateEmailSettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Create)]
    [HttpPost("test")]
    public async Task<IActionResult> TestEmailSettings(
        [FromBody] TestEmailSettingsBody? body,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new TestEmailSettingsCommand(body?.TestToEmail), cancellationToken);

        return Ok(new { success = true });
    }

    public sealed record TestEmailSettingsBody(string? TestToEmail);
}
