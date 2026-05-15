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

public sealed record UpdateRolePermissionsCommand(Guid RoleId, IReadOnlyList<RolePermissionRequest> Permissions) : IRequest<SuccessResponse>;

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
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

public sealed class UpdateRolePermissionsCommandHandler : IRequestHandler<UpdateRolePermissionsCommand, SuccessResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRolePermissionsCommandHandler(
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

    public async Task<SuccessResponse> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.RolesModuleCode,
            PermissionAction.Update,
            cancellationToken);

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role was not found");

        if (role.OrganizationId != organizationId)
        {
            throw new ForbiddenException("Role belongs to another organization");
        }

        if (role.Name.Equals(IdentityApplicationConstants.AdminRoleName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Admin role permissions cannot be changed");
        }

        var oldPermissions = await _moduleRoleRepository.GetByRoleIdAsync(role.Id, cancellationToken);
        var newPermissions = await BuildModuleRolesAsync(role.Id, organizationId, request.Permissions, cancellationToken);
        var oldSnapshot = IdentityAuditSnapshots.RolePermissions(oldPermissions);

        _moduleRoleRepository.DeleteRange(oldPermissions);
        await _moduleRoleRepository.AddRangeAsync(newPermissions, cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Roles",
            AuditAction.PermissionChange,
            "Role",
            role.Id,
            $"Role {role.Name} permissions were updated",
            oldSnapshot,
            IdentityAuditSnapshots.RolePermissions(newPermissions),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }

    private async Task<IReadOnlyList<ModuleRole>> BuildModuleRolesAsync(
        Guid roleId,
        Guid organizationId,
        IReadOnlyList<RolePermissionRequest> permissions,
        CancellationToken cancellationToken)
    {
        var result = new List<ModuleRole>();

        foreach (var permission in permissions)
        {
            var module = await _moduleRepository.GetByCodeAsync(permission.ModuleCode, cancellationToken)
                ?? throw new NotFoundException($"Module '{permission.ModuleCode}' was not found");

            result.Add(new ModuleRole(
                Guid.NewGuid(),
                organizationId,
                roleId,
                module.Id,
                permission.CanRead,
                permission.CanCreate,
                permission.CanUpdate,
                permission.CanDelete));
        }

        return result;
    }
}
