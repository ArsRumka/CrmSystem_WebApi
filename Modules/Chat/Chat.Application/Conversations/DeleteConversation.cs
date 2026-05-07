using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Conversations;

public sealed record DeleteConversationCommand(Guid Id) : IRequest;

public sealed class DeleteConversationCommandValidator : AbstractValidator<DeleteConversationCommand>
{
    public DeleteConversationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class DeleteConversationCommandHandler : IRequestHandler<DeleteConversationCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteConversationCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteConversationCommand request, CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Delete, cancellationToken);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        conversation.SoftDelete(userId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
