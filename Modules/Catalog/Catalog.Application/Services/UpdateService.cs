using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
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

public sealed record UpdateServiceCommand(
    Guid Id,
    Guid? CategoryId,
    string Name,
    string? Description,
    decimal Price,
    BonusType BonusType,
    decimal? BonusValue,
    DiscountType DiscountType,
    decimal? DiscountValue) : IRequest<ServiceResponse>;

public sealed class UpdateServiceCommandValidator : AbstractValidator<UpdateServiceCommand>
{
    public UpdateServiceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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

public sealed class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateServiceCommandHandler(
        ICurrentUserService currentUserService,
        ICategoryRepository categoryRepository,
        IServiceRepository serviceRepository,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _categoryRepository = categoryRepository;
        _serviceRepository = serviceRepository;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResponse> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var organizationId = CatalogApplicationGuards.RequireOrganizationUser(_currentUserService);

        var service = await _serviceRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Service was not found");

        if (request.CategoryId.HasValue &&
            !await _categoryRepository.ExistsByIdAsync(organizationId, request.CategoryId.Value, cancellationToken))
        {
            throw new NotFoundException("Category was not found");
        }

        var oldValues = new
        {
            service.Name,
            service.CategoryId,
            service.Price,
            service.BonusType,
            service.BonusValue,
            service.DiscountType,
            service.DiscountValue,
            service.IsActive
        };

        service.Update(
            request.CategoryId,
            request.Name,
            request.Description,
            request.Price,
            request.BonusType,
            request.BonusValue,
            request.DiscountType,
            request.DiscountValue,
            _dateTimeProvider.UtcNow);

        await _auditLogService.LogAsync(
            organizationId,
            _currentUserService.UserId,
            "Catalog",
            AuditAction.Update,
            "Service",
            service.Id,
            $"Service {service.Name} was updated",
            oldValues,
            newValues: new
            {
                service.Name,
                service.CategoryId,
                service.Price,
                service.BonusType,
                service.BonusValue,
                service.DiscountType,
                service.DiscountValue,
                service.IsActive
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return service.ToResponse();
    }
}
