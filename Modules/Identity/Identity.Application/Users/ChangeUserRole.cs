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

namespace Identity.Application.Users;

public sealed record ChangeUserRoleCommand(Guid UserId, Guid RoleId) : IRequest<SuccessResponse>;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class ChangeUserRoleCommandHandler : IRequestHandler<ChangeUserRoleCommand, SuccessResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeUserRoleCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.UsersModuleCode,
            PermissionAction.Update,
            cancellationToken);

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        if (user.OrganizationId != organizationId)
        {
            throw new ForbiddenException("User belongs to another organization");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role was not found");

        if (role.OrganizationId != organizationId)
        {
            throw new ForbiddenException("Role belongs to another organization");
        }

        user.ChangeRole(role.Id);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
