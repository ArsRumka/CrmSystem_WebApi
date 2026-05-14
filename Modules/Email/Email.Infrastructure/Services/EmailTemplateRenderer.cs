using Email.Application.Abstractions.Services;

namespace Email.Infrastructure.Services;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    public EmailTemplateRenderResult Render(EmailTemplateRenderRequest request)
    {
        var replacements = new Dictionary<string, string>
        {
            ["{{FirstName}}"] = request.FirstName ?? string.Empty,
            ["{{LastName}}"] = request.LastName ?? string.Empty,
            ["{{MiddleName}}"] = request.MiddleName ?? string.Empty,
            ["{{FullName}}"] = request.FullName ?? string.Empty,
            ["{{OrganizationName}}"] = request.OrganizationName ?? string.Empty,
            ["{{LastDealDate}}"] = request.LastDealDate?.ToString("yyyy-MM-dd") ?? string.Empty,
            ["{{DaysSinceLastDeal}}"] = request.DaysSinceLastDeal?.ToString() ?? string.Empty
        };

        return new EmailTemplateRenderResult(
            ReplaceKnownPlaceholders(request.Subject, replacements),
            ReplaceKnownPlaceholders(request.Body, replacements));
    }

    private static string ReplaceKnownPlaceholders(string value, IReadOnlyDictionary<string, string> replacements)
    {
        var result = value;

        foreach (var replacement in replacements)
        {
            result = result.Replace(replacement.Key, replacement.Value, StringComparison.Ordinal);
        }

        return result;
    }
}
