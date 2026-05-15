using Chat.Domain.Entities;

namespace Chat.Application.Common;

internal static class ChatAuditSnapshots
{
    public static object Conversation(ChatConversation conversation)
    {
        return new
        {
            conversation.Type,
            conversation.OwnerOrganizationId,
            conversation.ClientId,
            conversation.DealId,
            conversation.IsActive,
            ParticipantCount = conversation.Participants.Count,
            OrganizationCount = conversation.Organizations.Count
        };
    }

    public static object Participant(ChatParticipant participant)
    {
        return new
        {
            participant.ConversationId,
            participant.OrganizationId,
            participant.UserId,
            participant.IsActive
        };
    }

    public static object ContactRequest(ChatContactRequest contactRequest)
    {
        return new
        {
            contactRequest.RequesterOrganizationId,
            contactRequest.TargetOrganizationId,
            contactRequest.RequesterUserId,
            contactRequest.Status,
            contactRequest.ConversationId
        };
    }
}
