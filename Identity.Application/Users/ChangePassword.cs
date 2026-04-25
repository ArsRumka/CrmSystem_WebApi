using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using Identity.Application.Abstractions.Repositories;
using Identity.Application.Abstractions.Security;
using Identity.Application.Common;
using Identity.Application.Contracts;
using MediatR;

namespace Identity.Application.Users;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<SuccessResponse>;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MinimumLength(6);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, SuccessResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<SuccessResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = HandlerGuards.RequireUserId(_currentUserService);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found");

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is invalid");
        }

        user.ChangePassword(_passwordHasher.Hash(request.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SuccessResponse(true);
    }
}
