using Chat.Application.Contracts;
using Chat.Application.Conversations;
using Chat.Application.Messages;
using Chat.Application.Participants;
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
[Route("api/chat/conversations")]
public sealed class ChatConversationsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatConversationsController(IMediator mediator, IHubContext<ChatHub> hubContext)
    {
        _mediator = mediator;
        _hubContext = hubContext;
    }

    [RequirePermission("Chat", PermissionAction.Read)]
    [HttpGet]
    public async Task<IActionResult> GetConversations(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetConversationsQuery(activeOnly), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetConversationById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetConversationByIdQuery(id), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Create)]
    [HttpPost]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationBody body,
        CancellationToken cancellationToken)
    {
        var command = new CreateConversationCommand(
            body.Type,
            body.Title,
            body.ParticipantUserIds,
            body.ClientId,
            body.DealId);

        return Ok(await _mediator.Send(command, cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateConversation(
        Guid id,
        [FromBody] UpdateConversationBody body,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new UpdateConversationCommand(id, body.Title), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteConversationCommand(id), cancellationToken);
        return NoContent();
    }

    [RequirePermission("Chat", PermissionAction.Read)]
    [HttpGet("{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] DateTime? before,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetMessagesQuery(id, before, limit), cancellationToken));
    }

    [RequirePermission("Chat", PermissionAction.Create)]
    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(
        Guid id,
        [FromBody] SendMessageBody body,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new SendMessageCommand(id, body.Text), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(id))
            .SendAsync("MessageReceived", response, cancellationToken);

        return Ok(response);
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkConversationRead(
        Guid id,
        [FromBody] MarkConversationReadBody body,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new MarkConversationReadCommand(id, body.MessageId), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(id))
            .SendAsync("ConversationRead", response, cancellationToken);

        return Ok(response);
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpPost("{id:guid}/participants")]
    public async Task<IActionResult> AddParticipant(
        Guid id,
        [FromBody] AddParticipantBody body,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new AddParticipantCommand(id, body.UserId), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(id))
            .SendAsync("ParticipantAdded", response, cancellationToken);

        return Ok(response);
    }

    [RequirePermission("Chat", PermissionAction.Update)]
    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<IActionResult> RemoveParticipant(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RemoveParticipantCommand(id, userId), cancellationToken);

        await _hubContext.Clients
            .Group(ChatHub.ConversationGroup(id))
            .SendAsync("ParticipantRemoved", response, cancellationToken);

        return Ok(response);
    }

    public sealed record CreateConversationBody(
        ChatConversationType Type,
        string? Title,
        IReadOnlyList<Guid> ParticipantUserIds,
        Guid? ClientId,
        Guid? DealId);

    public sealed record UpdateConversationBody(string? Title);

    public sealed record SendMessageBody(string Text);

    public sealed record MarkConversationReadBody(Guid? MessageId);

    public sealed record AddParticipantBody(Guid UserId);
}
