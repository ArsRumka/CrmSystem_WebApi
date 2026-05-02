using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Auth;

public sealed record LoginCommand(string OrganizationEmail, string UserEmail, string Password) : IRequest<AuthTokenResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.OrganizationEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthTokenResponse>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IRefreshTokenRepository refreshTokenRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByEmailAsync(request.OrganizationEmail, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");

        if (!organization.IsActive)
        {
            throw new UnauthorizedException("Organization is inactive");
        }

        var user = await _userRepository.GetByEmailAsync(organization.Id, request.UserEmail, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User is inactive");
        }

        if (!user.IsEmailConfirmed)
        {
            throw new UnauthorizedException("Email is not confirmed");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken)
            ?? throw new UnauthorizedException("User role is not available");

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, role, organization);
        var refreshTokenPlain = _refreshTokenGenerator.Generate();
        var refreshToken = new RefreshToken(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(refreshTokenPlain),
            _refreshTokenGenerator.GetExpiration(),
            _dateTimeProvider.UtcNow);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthTokenResponse(accessToken, refreshTokenPlain, _jwtTokenGenerator.GetAccessTokenExpiration());
    }
}
