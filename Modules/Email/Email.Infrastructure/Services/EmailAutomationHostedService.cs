using Email.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Email.Infrastructure.Services;

public sealed class EmailAutomationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailAutomationOptions _options;
    private readonly ILogger<EmailAutomationHostedService> _logger;

    public EmailAutomationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailAutomationOptions> options,
        ILogger<EmailAutomationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation("Email automation hosted service is disabled");
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, _options.IntervalHours));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IEmailAutomationRunner>();

                await runner.RunAllAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email automation hosted service run failed");
            }
        }
    }
}
