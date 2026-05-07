using BuildingBlocks.Application.Abstractions.Auth;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using Chat.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Chat.Application.ContactRequests;

public sealed record GetOutgoingContactRequestsQuery(ChatContactRequestStatus? Status)
    : IRequest<IReadOnlyList<ChatContactRequestResponse>>;

public sealed class GetOutgoingContactRequestsQueryValidator : AbstractValidator<GetOutgoingContactRequestsQuery>
{
    public GetOutgoingContactRequestsQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
    }
}

public sealed class GetOutgoingContactRequestsQueryHandler
    : IRequestHandler<GetOutgoingContactRequestsQuery, IReadOnlyList<ChatContactRequestResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatContactRequestRepository _contactRequestRepository;
    private readonly ChatResponseFactory _responseFactory;

    public GetOutgoingContactRequestsQueryHandler(
        ICurrentUserService currentUserService,
        IChatContactRequestRepository contactRequestRepository,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _contactRequestRepository = contactRequestRepository;
        _responseFactory = responseFactory;
    }

    public async Task<IReadOnlyList<ChatContactRequestResponse>> Handle(
        GetOutgoingContactRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);

        var requests = await _contactRequestRepository.GetOutgoingAsync(
            organizationId,
            request.Status,
            cancellationToken);

        return await _responseFactory.CreateContactRequestResponsesAsync(requests, cancellationToken);
    }
}
