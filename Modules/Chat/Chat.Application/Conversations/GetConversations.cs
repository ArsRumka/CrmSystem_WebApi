using BuildingBlocks.Application.Abstractions.Auth;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Chat.Application.Conversations;

public sealed record GetConversationsQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<ChatConversationResponse>>;

public sealed class GetConversationsQueryValidator : AbstractValidator<GetConversationsQuery>
{
    public GetConversationsQueryValidator()
    {
    }
}

public sealed class GetConversationsQueryHandler
    : IRequestHandler<GetConversationsQuery, IReadOnlyList<ChatConversationResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly ChatResponseFactory _responseFactory;

    public GetConversationsQueryHandler(
        ICurrentUserService currentUserService,
        IChatConversationRepository conversationRepository,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _conversationRepository = conversationRepository;
        _responseFactory = responseFactory;
    }

    public async Task<IReadOnlyList<ChatConversationResponse>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);

        var conversations = await _conversationRepository.GetForUserAsync(
            organizationId,
            userId,
            request.ActiveOnly,
            cancellationToken);

        return await _responseFactory.CreateConversationResponsesAsync(conversations, userId, cancellationToken);
    }
}
