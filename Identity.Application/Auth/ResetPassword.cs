using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.Auth;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<SuccessResponse>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, SuccessResponse>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = _tokenHasher.Hash(request.Token);
        var token = await _passwordResetTokenRepository.GetByHashAsync(tokenHash, cancellationToken)
            ?? throw new NotFoundException("Password reset token was not found");

        if (token.UsedAt.HasValue)
        {
            throw new ConflictException("Password reset token is already used");
        }

        if (token.IsExpired(now))
        {
            throw new ConflictException("Password reset token is expired");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        token.MarkAsUsed(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
