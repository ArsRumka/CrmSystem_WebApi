namespace Email.Infrastructure.Services;

public sealed class EmailAutomationOptions
{
    public bool IsEnabled { get; init; } = true;

    public int IntervalHours { get; init; } = 24;
}
