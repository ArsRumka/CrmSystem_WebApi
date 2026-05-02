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
using MediatR;

namespace Identity.Application.Auth;

public sealed record RegisterOrganizationCommand(
    string ActivationKey,
    string OrganizationName,
    string OrganizationEmail,
    string AdminName,
    string AdminEmail,
    string AdminPassword) : IRequest<RegisterOrganizationResponse>;

public sealed class RegisterOrganizationCommandValidator : AbstractValidator<RegisterOrganizationCommand>
{
    public RegisterOrganizationCommandValidator()
    {
        RuleFor(x => x.ActivationKey).NotEmpty();
        RuleFor(x => x.OrganizationName).NotEmpty();
        RuleFor(x => x.OrganizationEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminName).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class RegisterOrganizationCommandHandler
    : IRequestHandler<RegisterOrganizationCommand, RegisterOrganizationResponse>
{
    private readonly IActivationKeyRepository _activationKeyRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IModuleRepository _moduleRepository;
    private readonly IModuleRoleRepository _moduleRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEmailConfirmationTokenRepository _emailConfirmationTokenRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterOrganizationCommandHandler(
        IActivationKeyRepository activationKeyRepository,
        IOrganizationRepository organizationRepository,
        IRoleRepository roleRepository,
        IModuleRepository moduleRepository,
        IModuleRoleRepository moduleRoleRepository,
        IUserRepository userRepository,
        IEmailConfirmationTokenRepository emailConfirmationTokenRepository,
        ITokenHasher tokenHasher,
        IPasswordHasher passwordHasher,
        ITokenGenerator tokenGenerator,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _activationKeyRepository = activationKeyRepository;
        _organizationRepository = organizationRepository;
        _roleRepository = roleRepository;
        _moduleRepository = moduleRepository;
        _moduleRoleRepository = moduleRoleRepository;
        _userRepository = userRepository;
        _emailConfirmationTokenRepository = emailConfirmationTokenRepository;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<RegisterOrganizationResponse> Handle(RegisterOrganizationCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var activationKeyHash = _tokenHasher.Hash(request.ActivationKey);
        var activationKey = await _activationKeyRepository.GetByHashAsync(activationKeyHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid activation key");

        if (activationKey.IsUsed)
        {
            throw new ConflictException("Activation key is already used");
        }

        if (activationKey.IsExpired(now))
        {
            throw new ConflictException("Activation key is expired");
        }

        if (await _organizationRepository.ExistsByEmailAsync(request.OrganizationEmail, cancellationToken))
        {
            throw new ConflictException("Organization email is already registered");
        }

        var modules = await _moduleRepository.GetAllAsync(cancellationToken);
        if (modules.Count == 0)
        {
            throw new NotFoundException("System modules are not configured");
        }

        var organization = new Organization(Guid.NewGuid(), request.OrganizationName, request.OrganizationEmail, activationKeyHash);
        var adminRole = new Role(Guid.NewGuid(), organization.Id, IdentityApplicationConstants.AdminRoleName);
        var adminUser = new User(
            Guid.NewGuid(),
            organization.Id,
            adminRole.Id,
            request.AdminName,
            request.AdminEmail,
            _passwordHasher.Hash(request.AdminPassword));

        var moduleRoles = modules.Select(module => new ModuleRole(
            Guid.NewGuid(),
            organization.Id,
            adminRole.Id,
            module.Id,
            canRead: true,
            canCreate: true,
            canUpdate: true,
            canDelete: true));

        var emailConfirmationTokenPlain = _tokenGenerator.GenerateSecureToken();
        var emailConfirmationToken = new EmailConfirmationToken(
            Guid.NewGuid(),
            adminUser.Id,
            _tokenHasher.Hash(emailConfirmationTokenPlain),
            now.AddHours(IdentityApplicationConstants.EmailConfirmationTokenLifetimeHours),
            now);

        activationKey.MarkAsUsed(now);

        await _organizationRepository.AddAsync(organization, cancellationToken);
        await _roleRepository.AddAsync(adminRole, cancellationToken);
        await _moduleRoleRepository.AddRangeAsync(moduleRoles, cancellationToken);
        await _userRepository.AddAsync(adminUser, cancellationToken);
        await _emailConfirmationTokenRepository.AddAsync(emailConfirmationToken, cancellationToken);
        await _emailSender.SendAsync(
            adminUser.Email,
            "Confirm your CRM account email",
            $"Use this confirmation token: {emailConfirmationTokenPlain}",
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterOrganizationResponse(organization.Id);
    }
}
