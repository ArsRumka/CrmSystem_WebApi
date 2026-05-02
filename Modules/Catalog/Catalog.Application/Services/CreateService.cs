using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Catalog.Application.Abstractions.Repositories;
using Catalog.Application.Common;
using Catalog.Application.Contracts;
using Catalog.Domain.Enums;
using FluentValidation;
using MediatR;

namespace Catalog.Application.Services;

public sealed record CreateServiceCommand(
    Guid? CategoryId,
    string Name,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue) : IRequest<ServiceResponse>;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEqual(Guid.Empty).When(x => x.CategoryId.HasValue);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BonusType).IsInEnum();
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x)
            .Must(x => CatalogValidationRules.IsValidBonusRule(x.BonusType, x.BonusValue))
            .WithMessage("Invalid bonus rule");
        RuleFor(x => x)
            .Must(x => CatalogValidationRules.IsValidDiscountRule(x.DiscountType, x.DiscountValue))
            .WithMessage("Invalid discount rule");
    }
}

public sealed class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateServiceCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IServiceRepository serviceRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _serviceRepository = serviceRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        if (request.CategoryId.HasValue &&
            !await _categoryRepository.ExistsByIdAsync(organizationId, request.CategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("Category was not found");
        }

        var service = new Domain.Entities.Service(
            Guid.NewGuid(),
            organizationId,
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.BonusType,
            request.BonusValue,
            request.DiscountType,
            request.DiscountValue,
            _dateTimeProvider.UtcNow);

        await _serviceRepository.AddAsync(service, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return service.ToResponse();
    }
}
