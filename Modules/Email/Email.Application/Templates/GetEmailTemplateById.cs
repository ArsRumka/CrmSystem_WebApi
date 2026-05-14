using BuildingBlocks.Application.Abstractions.Auth;
using BuildingBlocks.Application.Exceptions;
using Email.Application.Abstractions.Repositories;
using Email.Application.Common;
using Email.Application.Contracts;
using FluentValidation;
using MediatR;

namespace Email.Application.Templates;

public sealed record GetEmailTemplateByIdQuery(Guid Id) : IRequest<EmailTemplateResponse>;

public sealed class GetEmailTemplateByIdQueryValidator : AbstractValidator<GetEmailTemplateByIdQuery>
{
    public GetEmailTemplateByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class GetEmailTemplateByIdQueryHandler
    : IRequestHandler<GetEmailTemplateByIdQuery, EmailTemplateResponse>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailTemplateRepository _templateRepository;

    public GetEmailTemplateByIdQueryHandler(
        ICurrentUserService currentUserService,
        IEmailTemplateRepository templateRepository)
    {
        _currentUserService = currentUserService;
        _templateRepository = templateRepository;
    }

    public async Task<EmailTemplateResponse> Handle(
        GetEmailTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        var (organizationId, _) = EmailApplicationGuards.RequireOrganizationUser(_currentUserService);

        var template = await _templateRepository.GetByIdAsync(organizationId, request.Id, cancellationToken)
            ?? throw new NotFoundException("Email template was not found");

        return template.ToResponse();
    }
}
