using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Clients.Application.Abstractions.Repositories;
using Clients.Application.Common;
using Clients.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Clients.Application.Clients;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<ClientResponse>;

public sealed class GetClientByIdQueryValidator : AbstractValidator<GetClientByIdQuery>
{
    public GetClientByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientRepository _clientRepository;

    public GetClientByIdQueryHandler(
        ICurrentUserService currentUserService,
        IClientRepository clientRepository)
    {
        _currentUserService = currentUserService;
        _clientRepository = clientRepository;
    }

    public async Task<ClientResponse> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var organizationId = ClientsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var client = await _clientRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Client was not found");

        return client.ToResponse();
    }
}
