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

public sealed record DeactivateUserCommand(Guid UserId) : IRequest<SuccessResponse>;

public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, SuccessResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.UsersModuleCode,
            PermissionAction.Delete,
            cancellationToken);

        if (request.UserId == currentUserId)
        {
            throw new ConflictException("Cannot deactivate yourself");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        if (user.OrganizationId != organizationId)
        {
            throw new ForbiddenException("User belongs to another organization");
        }

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
