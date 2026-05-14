using Email.Application.Campaigns;
using Email.Domain.Enums;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Email.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/email/campaigns")]
public sealed class EmailCampaignsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailCampaignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetEmailCampaigns(
        [FromQuery] EmailCampaignType? type,
        [FromQuery] EmailCampaignStatus? status,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetEmailCampaignsQuery(type, status, dateFrom, dateTo),
            cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmailCampaignById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailCampaignByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Read)]
    [HttpGet("{id:guid}/recipients")]
    public async Task<IActionResult> GetEmailCampaignRecipients(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetEmailCampaignRecipientsQuery(id), cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Create)]
    [HttpPost("manual")]
    public async Task<IActionResult> CreateManualEmailCampaign(
        [FromBody] CreateManualEmailCampaignCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Email", PermissionAction.Create)]
    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> SendEmailCampaign(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new SendEmailCampaignCommand(id), cancellationToken));
    }
}
