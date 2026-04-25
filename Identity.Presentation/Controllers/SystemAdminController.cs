using Identity.Application.SystemAdmins;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Presentation.Controllers;

[ApiController]
[Route("api/identity/system-admin")]
public sealed class SystemAdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemAdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] SystemAdminLoginCommand command, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpGet("organization-requests")]
    public async Task<IActionResult> GetOrganizationRequests(
        [FromQuery] OrganizationRequestStatus? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetOrganizationRequestsQuery(status), cancellationToken));
    }

    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpPost("organization-requests/{id:guid}/approve")]
    public async Task<IActionResult> ApproveOrganizationRequest(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ApproveOrganizationRequestCommand(id), cancellationToken));
    }

    [Authorize(Policy = "RequireSystemAdmin")]
    [HttpPost("organization-requests/{id:guid}/reject")]
    public async Task<IActionResult> RejectOrganizationRequest(
        Guid id,
        [FromBody] RejectOrganizationRequestBody? body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new RejectOrganizationRequestCommand(id, body?.Reason), cancellationToken));
    }

    public sealed record RejectOrganizationRequestBody(string? Reason);
}
