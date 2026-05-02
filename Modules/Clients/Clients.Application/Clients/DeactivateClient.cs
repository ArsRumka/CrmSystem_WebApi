using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Clients.Application.Abstractions.Repositories;
using Clients.Application.Common;
using FluentValidation;
using MediatR;

namespace Clients.Application.Clients;

public sealed record DeactivateClientCommand(Guid Id) : IRequest;

public sealed class DeactivateClientCommandValidator : AbstractValidator<DeactivateClientCommand>
{
    public DeactivateClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateClientCommandHandler : IRequestHandler<DeactivateClientCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientRepository _clientRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateClientCommandHandler(
        ICurrentUserService currentUserService,
        IClientRepository clientRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _clientRepository = clientRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateClientCommand request, CancellationToken cancellationToken)
    {
        var organizationId = ClientsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var client = await _clientRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Client was not found");

        client.Deactivate(_dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
