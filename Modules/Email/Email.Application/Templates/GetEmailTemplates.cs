using BuildingBlocks.Application.Abstractions.Auth;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Templates;

public sealed record GetEmailTemplatesQuery(bool? IsActive, string? Search) : IRequest<IReadOnlyList<EmailTemplateResponse>>;

public sealed class GetEmailTemplatesQueryValidator : AbstractValidator<GetEmailTemplatesQuery>
{
    public GetEmailTemplatesQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class GetEmailTemplatesQueryHandler
    : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;

    public GetEmailTemplatesQueryHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
    }

    public async Task<IReadOnlyList<EmailTemplateResponse>> Handle(
        GetEmailTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var templates = await _templateRepository.SearchAsync(
            organizationId,
            request.IsActive,
            request.Search,
            cancellationToken);

        return templates.Select(x => x.ToResponse()).ToList();
    }
}
