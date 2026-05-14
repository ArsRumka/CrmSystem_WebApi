using Email.Application.Templates;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Email.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/email/templates")]
public sealed class EmailTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetEmailTemplates(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailTemplatesQuery(isActive, search), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmailTemplateById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailTemplateByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateEmailTemplate(
        [FromBody] CreateEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEmailTemplate(
        Guid id,
        [FromBody] UpdateEmailTemplateBody body,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmailTemplateCommand(
            id,
            body.Name,
            body.Subject,
            body.Body,
            body.IsHtml,
            body.IsActive);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateEmailTemplate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateEmailTemplateCommand(id), cancellationToken);

        return NoContent();
    }

    public sealed record UpdateEmailTemplateBody(
        string Name,
        string Subject,
        string Body,
        bool IsHtml,
        bool IsActive);
}
