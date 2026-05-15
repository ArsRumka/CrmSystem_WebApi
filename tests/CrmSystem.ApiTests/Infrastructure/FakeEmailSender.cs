using System.Collections.Concurrent;
using BuildingBlocks.Application.Abstractions.Email;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<FakeEmailMessage> _messages = new();

    public IReadOnlyCollection<FakeEmailMessage> SentMessages => _messages.ToArray();

    public Task SendAsync(
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(new FakeEmailMessage(to, subject, body));
        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (_messages.TryDequeue(out _))
        {
        }
    }
}

public sealed record FakeEmailMessage(string To, string Subject, string Body);
