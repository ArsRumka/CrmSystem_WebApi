using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Roles;

public sealed record CreateRoleCommand(string Name, IReadOnlyList<RolePermissionRequest> Permissions) : IRequest<RoleResponse>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Permissions).NotNull().NotEmpty();
        RuleForEach(x => x.Permissions).ChildRules(permission =>
        {
            permission.RuleFor(x => x.ModuleCode).NotEmpty();
        });
        RuleFor(x => x.Permissions)
            .Must(HaveDistinctModuleCodes)
            .WithMessage("Permissions must contain distinct module codes");
    }

    private static bool HaveDistinctModuleCodes(IReadOnlyList<RolePermissionRequest>? permissions)
    {
        if (permissions is null)
        {
            return true;
        }

        return permissions
            .Select(permission => permission.ModuleCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == permissions.Count;
    }
}

public sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IRoleRepository roleRepository,
        IModuleRepository moduleRepository,
        IModuleRoleRepository moduleRoleRepository,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _roleRepository = roleRepository;
        _moduleRepository = moduleRepository;
        _moduleRoleRepository = moduleRoleRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.RolesModuleCode,
            PermissionAction.Create,
            cancellationToken);

        if (await _roleRepository.ExistsByNameAsync(organizationId, request.Name, cancellationToken))
        {
            throw new ConflictException("Role name is already used in this organization");
        }

        var role = new Role(Guid.NewGuid(), organizationId, request.Name);
        var moduleRoles = new List<ModuleRole>();
        var responsePermissions = new List<RolePermissionResponse>();

        foreach (var permission in request.Permissions)
        {
            var module = await _moduleRepository.GetByCodeAsync(permission.ModuleCode, cancellationToken)
                ?? throw new NotFoundException($"Module '{permission.ModuleCode}' was not found");

            moduleRoles.Add(new ModuleRole(
                Guid.NewGuid(),
                organizationId,
                role.Id,
                module.Id,
                permission.CanRead,
                permission.CanCreate,
                permission.CanUpdate,
                permission.CanDelete));

            responsePermissions.Add(new RolePermissionResponse(
                module.Id,
                module.Code,
                module.Name,
                permission.CanRead,
                permission.CanCreate,
                permission.CanUpdate,
                permission.CanDelete));
        }

        await _roleRepository.AddAsync(role, cancellationToken);
        await _moduleRoleRepository.AddRangeAsync(moduleRoles, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Roles",
            AuditAction.Create,
            "Role",
            role.Id,
            $"Role {role.Name} was created",
            oldValues: null,
            newValues: IdentityAuditSnapshots.Role(role, responsePermissions),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RoleResponse(role.Id, role.OrganizationId, role.Name, responsePermissions);
    }
}
