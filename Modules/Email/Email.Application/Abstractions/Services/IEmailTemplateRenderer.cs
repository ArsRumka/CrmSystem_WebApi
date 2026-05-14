namespace Email.Application.Abstractions.Services;

public interface IEmailTemplateRenderer
{
    EmailTemplateRenderResult Render(EmailTemplateRenderRequest request);
}

public sealed record EmailTemplateRenderRequest(
    string Subject,
    string Body,
    string? FirstName,
    string? LastName,
    string? MiddleName,
    string? FullName,
    string? OrganizationName,
    DateTime? LastDealDate,
    int? DaysSinceLastDeal);

public sealed record EmailTemplateRenderResult(string Subject, string Body);
