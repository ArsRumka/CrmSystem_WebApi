using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.SystemAdmins;

public sealed record SystemAdminLoginCommand(string Email, string Password) : IRequest<AccessTokenResponse>;

public sealed class SystemAdminLoginCommandValidator : AbstractValidator<SystemAdminLoginCommand>
{
    public SystemAdminLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public sealed class SystemAdminLoginCommandHandler : IRequestHandler<SystemAdminLoginCommand, AccessTokenResponse>
{
    private readonly ISystemAdminRepository _systemAdminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public SystemAdminLoginCommandHandler(
        ISystemAdminRepository systemAdminRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _systemAdminRepository = systemAdminRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AccessTokenResponse> Handle(SystemAdminLoginCommand request, CancellationToken cancellationToken)
    {
        var systemAdmin = await _systemAdminRepository.GetByEmailAsync(request.Email, cancellationToken)
            ?? throw new UnauthorizedException("Invalid credentials");

        if (!systemAdmin.IsActive)
        {
            throw new UnauthorizedException("System administrator is inactive");
        }

        if (!_passwordHasher.Verify(request.Password, systemAdmin.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials");
        }

        return new AccessTokenResponse(
            _jwtTokenGenerator.GenerateSystemAdminAccessToken(systemAdmin),
            _jwtTokenGenerator.GetAccessTokenExpiration());
    }
}
