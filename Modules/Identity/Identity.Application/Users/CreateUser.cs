using Audit.Application.Abstractions.Services;
using Audit.Domain.Enums;
using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Email;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Users;

public sealed record CreateUserCommand(string Name, string Email, string Password, Guid RoleId) : IRequest<UserResponse>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IEmailConfirmationTokenRepository _emailConfirmationTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogService _auditLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IEmailConfirmationTokenRepository emailConfirmationTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        IAuditLogService auditLogService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _emailConfirmationTokenRepository = emailConfirmationTokenRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _auditLogService = auditLogService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = HandlerGuards.RequireUserId(_currentUserService);
        var organizationId = HandlerGuards.RequireOrganizationId(_currentUserService);
        await HandlerGuards.EnsurePermissionAsync(
            _permissionService,
            currentUserId,
            IdentityApplicationConstants.UsersModuleCode,
            PermissionAction.Create,
            cancellationToken);

        if (await _userRepository.ExistsByEmailAsync(organizationId, request.Email, cancellationToken))
        {
            throw new ConflictException("User email is already used in this organization");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role was not found");

        if (role.OrganizationId != organizationId)
        {
            throw new ForbiddenException("Role belongs to another organization");
        }

        var now = _dateTimeProvider.UtcNow;
        var user = new User(
            Guid.NewGuid(),
            organizationId,
            role.Id,
            request.Name,
            request.Email,
            _passwordHasher.Hash(request.Password));

        var emailConfirmationTokenPlain = _tokenGenerator.GenerateSecureToken();
        var emailConfirmationToken = new EmailConfirmationToken(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(emailConfirmationTokenPlain),
            now.AddHours(IdentityApplicationConstants.EmailConfirmationTokenLifetimeHours),
            now);

        await _userRepository.AddAsync(user, cancellationToken);
        await _emailConfirmationTokenRepository.AddAsync(emailConfirmationToken, cancellationToken);
        await _emailSender.SendAsync(
            user.Email,
            "Confirm your CRM account email",
            $"Use this confirmation token: {emailConfirmationTokenPlain}",
            cancellationToken);
        await _auditLogService.LogAsync(
            organizationId,
            currentUserId,
            "Users",
            AuditAction.Create,
            "User",
            user.Id,
            $"User {user.Name} was created",
            oldValues: null,
            newValues: IdentityAuditSnapshots.User(user),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.OrganizationId, user.RoleId, user.Name, user.Email, user.IsActive, user.IsEmailConfirmed);
    }
}
