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

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthTokenResponse>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IOrganizationRepository organizationRepository,
        ITokenHasher tokenHasher,
        IRefreshTokenGenerator refreshTokenGenerator,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _organizationRepository = organizationRepository;
        _tokenHasher = tokenHasher;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var refreshTokenHash = _tokenHasher.Hash(request.RefreshToken);
        var storedRefreshToken = await _refreshTokenRepository.GetByHashAsync(refreshTokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token");

        if (storedRefreshToken.RevokedAt.HasValue || storedRefreshToken.IsExpired(now))
        {
            throw new UnauthorizedException("Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(storedRefreshToken.UserId, cancellationToken)
            ?? throw new UnauthorizedException("User is not available");

        if (!user.IsActive || !user.IsEmailConfirmed)
        {
            throw new UnauthorizedException("User is not available");
        }

        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken)
            ?? throw new UnauthorizedException("User role is not available");

        var organization = await _organizationRepository.GetByIdAsync(user.OrganizationId, cancellationToken)
            ?? throw new UnauthorizedException("Organization is not available");

        if (!organization.IsActive)
        {
            throw new UnauthorizedException("Organization is inactive");
        }

        storedRefreshToken.Revoke(now);

        var newRefreshTokenPlain = _refreshTokenGenerator.Generate();
        var newRefreshToken = new Identity.Domain.Entities.RefreshToken(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(newRefreshTokenPlain),
            _refreshTokenGenerator.GetExpiration(),
            now);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user, role, organization);

        return new AuthTokenResponse(accessToken, newRefreshTokenPlain, _jwtTokenGenerator.GetAccessTokenExpiration());
    }
}
