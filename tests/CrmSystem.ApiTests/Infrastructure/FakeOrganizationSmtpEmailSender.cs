using System.Collections.Concurrent;
using Email.Application.Abstractions.Services;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class FakeOrganizationSmtpEmailSender : IOrganizationSmtpEmailSender
{
    private readonly ConcurrentQueue<OrganizationEmailMessage> _messages = new();

    public IReadOnlyCollection<OrganizationEmailMessage> SentMessages => _messages.ToArray();

    public Task SendAsync(OrganizationEmailMessage message, CancellationToken cancellationToken)
    {
        if (message.ToEmail.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Fake SMTP failure requested by recipient email.");
        }

        _messages.Enqueue(message);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}
