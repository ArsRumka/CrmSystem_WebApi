using Email.Application.Automation;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Email.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/email/automation")]
public sealed class EmailAutomationController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailAutomationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetEmailAutomationRule(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailAutomationRuleQuery(), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Update)]
    [HttpPut]
    public async Task<IActionResult> UpdateEmailAutomationRule(
        [FromBody] UpdateEmailAutomationRuleCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Create)]
    [HttpPost("run")]
    public async Task<IActionResult> RunEmailAutomationNow(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new RunEmailAutomationNowCommand(), cancellationToken));
    }
}
