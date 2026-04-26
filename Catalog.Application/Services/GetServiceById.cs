using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Services;

public sealed record GetServiceByIdQuery(Guid Id) : IRequest<ServiceResponse>;

public sealed class GetServiceByIdQueryValidator : AbstractValidator<GetServiceByIdQuery>
{
    public GetServiceByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetServiceByIdQueryHandler : IRequestHandler<GetServiceByIdQuery, ServiceResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceRepository _serviceRepository;

    public GetServiceByIdQueryHandler(
        ICurrentUserService currentUserService,
        IServiceRepository serviceRepository)
    {
        _currentUserService = currentUserService;
        _serviceRepository = serviceRepository;
    }

    public async Task<ServiceResponse> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var service = await _serviceRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Service was not found");

        return service.ToResponse();
    }
}
