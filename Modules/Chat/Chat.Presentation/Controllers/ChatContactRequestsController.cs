using Chat.Application.ContactRequests;
using Chat.Domain.Enums;
using Chat.Presentation.Hubs;
using Identity.Domain.Enums;
using Identity.Presentation.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/chat/contact-requests")]
public sealed class ChatContactRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatContactRequestsController(IMediator mediator, IHubContext<ChatHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    [RequirePermission("Chat", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateContactRequest(
        [FromBody] CreateContactRequestBody body,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new CreateContactRequestCommand(body.TargetOrganizationEmail, body.Message),
            cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.OrganizationGroup(response.TargetOrganizationId))
            .SendAsync("ContactRequestReceived", response, cancellationToken);

        return Ok(response);
    }

    [RequirePermission("Chat", PermissionAction.Read)]
    [HttpGet("incoming")]
    public async Task<IActionResult> GetIncomingContactRequests(
        [FromQuery] ChatContactRequestStatus? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetIncomingContactRequestsQuery(status), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Read)]
    [HttpGet("outgoing")]
    public async Task<IActionResult> GetOutgoingContactRequests(
        [FromQuery] ChatContactRequestStatus? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetOutgoingContactRequestsQuery(status), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> ApproveContactRequest(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ApproveContactRequestCommand(id), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectContactRequest(
        Guid id,
        [FromBody] RejectContactRequestBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new RejectContactRequestCommand(id, body.Reason), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelContactRequest(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new CancelContactRequestCommand(id), cancellationToken));
    }

    public sealed record CreateContactRequestBody(string TargetOrganizationEmail, string? Message);

    public sealed record RejectContactRequestBody(string? Reason);
}
