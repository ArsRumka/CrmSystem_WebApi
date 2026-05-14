using Email.Domain.Entities;

namespace Email.Application.Contracts;

public sealed record EmailTemplateResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Subject,
    string Body,
    bool IsHtml,
    bool IsActive,
    DateTime CreatedAt,
    Guid CreatedByUserId,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId);

internal static class EmailTemplateResponseMapper
{
    public static EmailTemplateResponse ToResponse(this EmailTemplate template)
    {
        return new EmailTemplateResponse(
            template.Id,
            template.OrganizationId,
            template.Name,
            template.Subject,
            template.Body,
            template.IsHtml,
            template.IsActive,
            template.CreatedAt,
            template.CreatedByUserId,
            template.UpdatedAt,
            template.UpdatedByUserId);
    }
}
