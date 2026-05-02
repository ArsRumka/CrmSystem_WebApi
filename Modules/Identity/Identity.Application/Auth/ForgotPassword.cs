using BuildingBlocks.Application.Abstractions.Email;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Auth;

public sealed record ForgotPasswordCommand(string OrganizationEmail, string UserEmail) : IRequest<SuccessResponse>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.OrganizationEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
    }
}

public sealed class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, SuccessResponse>
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IEmailSender _emailSender;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        ITokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IEmailSender emailSender,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _emailSender = emailSender;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var organization = await _organizationRepository.GetByEmailAsync(request.OrganizationEmail, cancellationToken);
        if (organization is null)
        {
            return new SuccessResponse(true);
        }

        var user = await _userRepository.GetByEmailAsync(organization.Id, request.UserEmail, cancellationToken);
        if (user is null)
        {
            return new SuccessResponse(true);
        }

        var now = _dateTimeProvider.UtcNow;
        var tokenPlain = _tokenGenerator.GenerateSecureToken();
        var token = new PasswordResetToken(
            Guid.NewGuid(),
            user.Id,
            _tokenHasher.Hash(tokenPlain),
            now.AddHours(IdentityApplicationConstants.PasswordResetTokenLifetimeHours),
            now);

        await _passwordResetTokenRepository.AddAsync(token, cancellationToken);
        await _emailSender.SendAsync(
            user.Email,
            "Reset your CRM password",
            $"Use this password reset token: {tokenPlain}",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
