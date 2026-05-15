using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Clients.Application.Abstractions.Repositories;
using Clients.Application.Common;
using Clients.Application.Contracts;
using Clients.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Clients.Application.Clients;

public sealed record UpdateClientCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? MiddleName,
    string? Email,
    string? Phone,
    ClientStatus Status,
    ClientSource Source,
    bool AllowMarketingEmails,
    string? Notes) : IRequest<ClientResponse>;

public sealed class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.Email)
            .MaximumLength(256)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Email or phone must be provided");
    }
}

public sealed class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, ClientResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IClientRepository _clientRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClientCommandHandler(
        ICurrentUserService currentUserService,
        IClientRepository clientRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _clientRepository = clientRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClientResponse> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var organizationId = ClientsApplicationGuards.RequireOrganizationUser(_currentUserService);

        var client = await _clientRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Client was not found");

        var oldValues = new
        {
            client.FirstName,
            client.LastName,
            client.MiddleName,
            client.Email,
            client.Phone,
            client.Status,
            client.Source,
            client.AllowMarketingEmails,
            client.IsActive
        };

        client.Update(
            request.FirstName,
            request.LastName,
            request.MiddleName,
            request.Email,
            request.Phone,
            request.Status,
            request.Source,
            request.AllowMarketingEmails,
            request.Notes,
            _dateTimeProvider.UtcNow);

        await _auditLogService.LogAsync(
            organizationId,
            _currentUserService.UserId,
            "Clients",
            AuditAction.Update,
            "Client",
            client.Id,
            $"Client {client.LastName} {client.FirstName} was updated",
            oldValues,
            newValues: new
            {
                client.FirstName,
                client.LastName,
                client.MiddleName,
                client.Email,
                client.Phone,
                client.Status,
                client.Source,
                client.AllowMarketingEmails,
                client.IsActive
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return client.ToResponse();
    }
}
