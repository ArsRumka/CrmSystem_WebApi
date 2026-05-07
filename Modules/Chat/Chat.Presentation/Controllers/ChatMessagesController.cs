using Chat.Application.Messages;
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
[Route("api/chat/messages")]
public sealed class ChatMessagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatMessagesController(IMediator mediator, IHubContext<ChatHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> EditMessage(
        Guid id,
        [FromBody] EditMessageBody body,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new EditMessageCommand(id, body.Text), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(response.ConversationId))
            .SendAsync("MessageEdited", response, cancellationToken);

        return Ok(response);
    }

    [RequirePermission("Chat", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid id, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new DeleteMessageCommand(id), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(response.ConversationId))
            .SendAsync("MessageDeleted", response, cancellationToken);

        return Ok(response);
    }

    public sealed record EditMessageBody(string Text);
}
