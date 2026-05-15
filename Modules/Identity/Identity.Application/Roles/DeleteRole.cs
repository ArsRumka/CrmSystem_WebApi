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
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Roles;

public sealed record DeleteRoleCommand(Guid RoleId) : IRequest<SuccessResponse>;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, SuccessResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.RolesModuleCode,
            PermissionAction.Delete,
            cancellationToken);

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role was not found");

        if (role.OrganizationId != organizationId)
        {
            throw new ForbiddenException("Role belongs to another organization");
        }

        if (role.Name.Equals(IdentityApplicationConstants.AdminRoleName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Admin role cannot be deleted");
        }

        var usersWithRole = await _userRepository.GetByRoleIdAsync(role.Id, cancellationToken);
        if (usersWithRole.Count > 0)
        {
            throw new ConflictException("Role cannot be deleted while users are assigned to it");
        }

        _roleRepository.Delete(role);
        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Roles",
            AuditAction.Delete,
            "Role",
            role.Id,
            $"Role {role.Name} was deleted",
            oldValues: new
            {
                role.Name
            },
            newValues: null,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
