using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Conversations;

public sealed record UpdateConversationCommand(Guid Id, string? Title) : IRequest<ChatConversationResponse>;

public sealed class UpdateConversationCommandValidator : AbstractValidator<UpdateConversationCommand>
{
    public UpdateConversationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(200);
    }
}

public sealed class UpdateConversationCommandHandler
    : IRequestHandler<UpdateConversationCommand, ChatConversationResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public UpdateConversationCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatConversationResponse> Handle(
        UpdateConversationCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        conversation.UpdateTitle(request.Title, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateConversationResponseAsync(conversation, userId, cancellationToken);
    }
}
