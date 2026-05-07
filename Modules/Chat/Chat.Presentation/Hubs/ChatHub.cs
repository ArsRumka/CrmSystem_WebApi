using System.Security.Claims;
using Chat.Application.Messages;
using Chat.Application.Realtime;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chat.Presentation.Hubs;

[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public static string ConversationGroup(Guid conversationId) => $"Conversation:{conversationId}";
    public static string UserGroup(Guid userId) => $"User:{userId}";
    public static string OrganizationGroup(Guid organizationId) => $"Organization:{organizationId}";

    public override async Task OnConnectedAsync()
    {
        var userId = GetGuidClaim("UserId");
        var organizationId = GetGuidClaim("OrganizationId");

        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId.Value), Context.ConnectionAborted);
        }

        if (organizationId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrganizationGroup(organizationId.Value), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var (organizationId, userId) = RequireActor();

        await _mediator.Send(
            new JoinConversationCommand(conversationId, organizationId, userId),
            Context.ConnectionAborted);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            ConversationGroup(conversationId),
            Context.ConnectionAborted);
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            ConversationGroup(conversationId),
            Context.ConnectionAborted);
    }

    public async Task SendMessage(Guid conversationId, string text)
    {
        var (organizationId, userId) = RequireActor();

        var response = await _mediator.Send(
            new SendMessageCommand(conversationId, text, organizationId, userId),
            Context.ConnectionAborted);

        await Clients
            .Group(ConversationGroup(conversationId))
            .SendAsync("MessageReceived", response, Context.ConnectionAborted);
    }

    public async Task MarkAsRead(Guid conversationId, Guid? messageId)
    {
        var (organizationId, userId) = RequireActor();

        var response = await _mediator.Send(
            new MarkConversationReadCommand(conversationId, messageId, organizationId, userId),
            Context.ConnectionAborted);

        await Clients
            .Group(ConversationGroup(conversationId))
            .SendAsync("ConversationRead", response, Context.ConnectionAborted);
    }

    public async Task Typing(Guid conversationId)
    {
        var (organizationId, userId) = RequireActor();

        var response = await _mediator.Send(
            new TypingCommand(conversationId, organizationId, userId),
            Context.ConnectionAborted);

        await Clients
            .OthersInGroup(ConversationGroup(conversationId))
            .SendAsync("UserTyping", response, Context.ConnectionAborted);
    }

    private (Guid OrganizationId, Guid UserId) RequireActor()
    {
        var organizationId = GetGuidClaim("OrganizationId");
        var userId = GetGuidClaim("UserId");

        if (organizationId is null || userId is null)
        {
            throw new HubException("Organization user is not authenticated");
        }

        return (organizationId.Value, userId.Value);
    }

    private Guid? GetGuidClaim(string claimType)
    {
        var value = Context.User?.FindFirstValue(claimType);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
