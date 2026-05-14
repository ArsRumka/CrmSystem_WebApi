using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using FluentValidation;
using MediatR;

namespace Email.Application.Templates;

public sealed record DeactivateEmailTemplateCommand(Guid Id) : IRequest;

public sealed class DeactivateEmailTemplateCommandValidator : AbstractValidator<DeactivateEmailTemplateCommand>
{
    public DeactivateEmailTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeactivateEmailTemplateCommandHandler : IRequestHandler<DeactivateEmailTemplateCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateEmailTemplateCommandHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var template = await _templateRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        template.Deactivate(_dateTimeProvider.UtcNow, userId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
