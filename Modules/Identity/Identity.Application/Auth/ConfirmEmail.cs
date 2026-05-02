using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.Auth;

public sealed record ConfirmEmailCommand(string Token) : IRequest<SuccessResponse>;

public sealed class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
    }
}

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, SuccessResponse>
{
    private readonly IEmailConfirmationTokenRepository _emailConfirmationTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(
        IEmailConfirmationTokenRepository emailConfirmationTokenRepository,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _emailConfirmationTokenRepository = emailConfirmationTokenRepository;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = _tokenHasher.Hash(request.Token);
        var token = await _emailConfirmationTokenRepository.GetByHashAsync(tokenHash, cancellationToken)
            ?? throw new NotFoundException("Email confirmation token was not found");

        if (token.UsedAt.HasValue)
        {
            throw new ConflictException("Email confirmation token is already used");
        }

        if (token.IsExpired(now))
        {
            throw new ConflictException("Email confirmation token is expired");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        user.ConfirmEmail();
        token.MarkAsUsed(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
