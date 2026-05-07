using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Abstractions.Persistence;
using BuildingBlocks.Application.Abstractions.Time;
using BuildingBlocks.Application.Exceptions;
using Chat.Application.Abstractions.Lookups;
using Chat.Application.Abstractions.Repositories;
using Chat.Application.Common;
using Chat.Application.Contracts;
using Chat.Domain.Entities;
using Chat.Domain.Enums;
using FluentValidation;
using Identity.Application.Abstractions.Security;
using Identity.Domain.Enums;
using MediatR;

namespace Chat.Application.Participants;

public sealed record AddParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<ChatParticipantResponse>;

public sealed class AddParticipantCommandValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class AddParticipantCommandHandler
    : IRequestHandler<AddParticipantCommand, ChatParticipantResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPermissionService _permissionService;
    private readonly IChatConversationRepository _conversationRepository;
    private readonly IChatParticipantRepository _participantRepository;
    private readonly IChatUserLookupService _userLookupService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ChatResponseFactory _responseFactory;

    public AddParticipantCommandHandler(
        ICurrentUserService currentUserService,
        IPermissionService permissionService,
        IChatConversationRepository conversationRepository,
        IChatParticipantRepository participantRepository,
        IChatUserLookupService userLookupService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ChatResponseFactory responseFactory)
    {
        _currentUserService = currentUserService;
        _permissionService = permissionService;
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _userLookupService = userLookupService;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _responseFactory = responseFactory;
    }

    public async Task<ChatParticipantResponse> Handle(
        AddParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var (organizationId, userId) = ChatApplicationGuards.RequireOrganizationUser(_currentUserService);
        await ChatApplicationGuards.RequirePermissionAsync(_permissionService, userId, PermissionAction.Update, cancellationToken);

        var conversation = await _conversationRepository.GetByIdWithDetailsAsync(request.ConversationId, cancellationToken)
            ?? throw new NotFoundException("Conversation was not found");

        ChatApplicationGuards.EnsureConversationActive(conversation);
        await ChatApplicationGuards.RequireActiveParticipantAsync(
            _participantRepository,
            conversation.Id,
            organizationId,
            userId,
            cancellationToken);

        if (conversation.Type == ChatConversationType.InterOrganization)
        {
            var organizationIsInConversation = conversation.Organizations.Any(x =>
                x.OrganizationId == organizationId &&
                x.IsActive);

            if (!organizationIsInConversation)
            {
                throw new ForbiddenException("Current organization is not active in this conversation");
            }
        }

        if (!await _userLookupService.ExistsActiveInOrganizationAsync(organizationId, request.UserId, cancellationToken))
        {
            throw new NotFoundException("User was not found");
        }

        var existingParticipant = await _participantRepository.GetAsync(
            conversation.Id,
            request.UserId,
            cancellationToken);
        var now = _dateTimeProvider.UtcNow;

        if (existingParticipant is not null)
        {
            if (existingParticipant.OrganizationId != organizationId)
            {
                throw new ConflictException("Existing participant belongs to another organization");
            }

            if (existingParticipant.IsActive)
            {
                throw new ConflictException("User is already an active participant");
            }

            existingParticipant.Reactivate(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await _responseFactory.CreateParticipantResponseAsync(existingParticipant, cancellationToken);
        }

        var participant = new ChatParticipant(
            Guid.NewGuid(),
            conversation.Id,
            organizationId,
            request.UserId,
            now);

        await _participantRepository.AddAsync(participant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await _responseFactory.CreateParticipantResponseAsync(participant, cancellationToken);
    }
}
