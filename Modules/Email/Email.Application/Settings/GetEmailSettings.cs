using BuildingBlocks.Application.Abstractions.Auth;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using MediatR;

namespace Email.Application.Settings;

public sealed record GetEmailSettingsQuery : IRequest<EmailSettingsResponse>;

public sealed class GetEmailSettingsQueryHandler : IRequestHandler<GetEmailSettingsQuery, EmailSettingsResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailSettingsRepository _settingsRepository;

    public GetEmailSettingsQueryHandler(
        ICurrentUserService currentUserService,
        IEmailSettingsRepository settingsRepository)
    {
        _currentUserService = currentUserService;
        _settingsRepository = settingsRepository;
    }

    public async Task<EmailSettingsResponse> Handle(
        GetEmailSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var settings = await _settingsRepository.GetByOrganizationIdAsync(organizationId, cancellationToken);

        return settings?.ToResponse() ?? EmailSettingsResponse.NotConfigured();
    }
}
