using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;

namespace Chat.Application.Common;

internal static class ChatApplicationGuards
{
    public const string ModuleCode = "Chat";

    public static (Guid OrganizationId, Guid UserId) RequireOrganizationUser(
        ICurrentUserService currentUserService,
        Guid? actorOrganizationId = null,
        Guid? actorUserId = null)
    {
        var organizationId = actorOrganizationId ?? currentUserService.OrganizationId;
        var userId = actorUserId ?? currentUserService.UserId;
        var hasExplicitActor = actorOrganizationId.HasValue && actorUserId.HasValue;

        if ((!hasExplicitActor && !currentUserService.IsAuthenticated) ||
            organizationId is null ||
            userId is null)
        {
            throw new UnauthorizedException("Organization user is not authenticated");
        }

        return (organizationId.Value, userId.Value);
    }

    public static async Task RequirePermissionAsync(
        IPermissionService permissionService,
        Guid userId,
        PermissionAction action,
        CancellationToken cancellationToken)
    {
        if (!await permissionService.HasPermissionAsync(userId, ModuleCode, action, cancellationToken))
        {
            throw new ForbiddenException("The authenticated user does not have the required Chat permission.");
        }
    }

    public static void EnsureConversationActive(ChatConversation conversation)
    {
        if (!conversation.IsActive || conversation.DeletedAt is not null)
        {
            throw new ConflictException("Conversation is not active");
        }
    }

    public static async Task<ChatParticipant> RequireParticipantAsync(
        IChatParticipantRepository participantRepository,
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await participantRepository.GetAsync(conversationId, userId, cancellationToken)
            ?? throw new ForbiddenException("User is not a participant of this conversation");
    }

    public static async Task<ChatParticipant> RequireActiveParticipantAsync(
        IChatParticipantRepository participantRepository,
        Guid conversationId,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var participant = await RequireParticipantAsync(participantRepository, conversationId, userId, cancellationToken);

        if (!participant.IsActive || participant.OrganizationId != organizationId)
        {
            throw new ForbiddenException("User is not an active participant of this conversation");
        }

        return participant;
    }

    public static void EnsureInterOrganizationTwoOrganizations(ChatConversation conversation)
    {
        if (conversation.Type != ChatConversationType.InterOrganization)
        {
            return;
        }

        var activeOrganizations = conversation.Organizations.Count(x => x.IsActive);
        if (activeOrganizations != 2)
        {
            throw new ConflictException("Inter-organization conversation must have exactly two active organizations");
        }
    }
}
