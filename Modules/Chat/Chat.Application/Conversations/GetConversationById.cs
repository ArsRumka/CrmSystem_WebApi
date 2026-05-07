using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Chat.Application.Conversations;

public sealed record GetConversationByIdQuery(Guid Id) : IRequest<ChatConversationResponse>;

public sealed class GetConversationByIdQueryValidator : AbstractValidator<GetConversationByIdQuery>
{
    public GetConversationByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetConversationByIdQueryHandler
    : IRequestHandler<GetConversationByIdQuery, ChatConversationResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly ChatResponseFactory _responseFactory;

    public GetConversationByIdQueryHandler(
        ICurrentUserService currentUserService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _responseFactory = responseFactory;
    }

    public async Task<ChatConversationResponse> Handle(
        GetConversationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (_, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        await ChatApplicationGuards.RequireParticipantAsync(
            _participantRepository,
            conversation.Id,
            userId,
            cancellationToken);

        return await _responseFactory.CreateConversationResponseAsync(conversation, userId, cancellationToken);
    }
}
