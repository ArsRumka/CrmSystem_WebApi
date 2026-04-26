using BuildingBlocks.Application.Abstractions.Auth;
using Clients.Application.Abstractions.Repositories;
using Clients.Application.Common;
using Clients.Application.Contracts;
using Clients.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Clients.Application.Clients;

public sealed record GetClientsQuery(
    string? Search,
    ClientStatus? Status,
    ClientSource? Source,
    bool? IsActive) : IRequest<IReadOnlyList<ClientResponse>>;

public sealed class GetClientsQueryValidator : AbstractValidator<GetClientsQuery>
{
    public GetClientsQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Source).IsInEnum().When(x => x.Source.HasValue);
    }
}

public sealed class GetClientsQueryHandler : IRequestHandler<GetClientsQuery, IReadOnlyList<ClientResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientRepository _clientRepository;

    public GetClientsQueryHandler(
        ICurrentUserService currentUserService,
        IClientRepository clientRepository)
    {
        _currentUserService = currentUserService;
        _clientRepository = clientRepository;
    }

    public async Task<IReadOnlyList<ClientResponse>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var organizationId = ClientsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var clients = await _clientRepository.SearchAsync(
            organizationId,
            request.Search,
            request.Status,
            request.Source,
            request.IsActive,
            cancellationToken);

        return clients
            .Select(client => client.ToResponse())
            .ToList();
    }
}
