using BuildingBlocks.Application.Abstractions.Time;

namespace CrmSystem.ApiTests.Infrastructure;

public sealed class TestClock : IDateTimeProvider
{
    private readonly object _lock = new();
    private DateTime _utcNow = DateTime.UtcNow;

    public DateTime UtcNow
    {
        get
        {
            lock (_lock)
            {
                return _utcNow;
            }
        }
    }

    public void SetUtcNow(DateTime utcNow)
    {
        lock (_lock)
        {
            _utcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        }
    }

    public void Reset()
    {
        SetUtcNow(DateTime.UtcNow);
    }
}
