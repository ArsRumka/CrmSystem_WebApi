using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Services;

public sealed record DeactivateServiceCommand(Guid Id) : IRequest;

public sealed class DeactivateServiceCommandValidator : AbstractValidator<DeactivateServiceCommand>
{
    public DeactivateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateServiceCommandHandler : IRequestHandler<DeactivateServiceCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceRepository _serviceRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateServiceCommandHandler(
        ICurrentUserService currentUserService,
        IServiceRepository serviceRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _serviceRepository = serviceRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateServiceCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var service = await _serviceRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Service was not found");

        service.Deactivate(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
