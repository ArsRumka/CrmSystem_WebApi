using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Chat.Application.Messages;

public sealed record GetMessagesQuery(Guid ConversationId, DateTime? Before, int Limit = 50)
    : IRequest<IReadOnlyList<ChatMessageResponse>>;

public sealed class GetMessagesQueryValidator : AbstractValidator<GetMessagesQuery>
{
    public GetMessagesQueryValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}

public sealed class GetMessagesQueryHandler
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<ChatMessageResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ChatResponseFactory _responseFactory;

    public GetMessagesQueryHandler(
        ICurrentUserService currentUserService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IChatMessageRepository messageRepository,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _responseFactory = responseFactory;
    }

    public async Task<IReadOnlyList<ChatMessageResponse>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken)
    {
        var (_, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);

        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        await ChatApplicationGuards.RequireParticipantAsync(
            _participantRepository,
            conversation.Id,
            userId,
            cancellationToken);

        var messages = await _messageRepository.GetByConversationIdAsync(
            conversation.Id,
            request.Before,
            request.Limit,
            cancellationToken);

        return await _responseFactory.CreateMessageResponsesAsync(messages, cancellationToken);
    }
}
