using BuildingBlocks.Application.Abstractions.Time;

namespace Infrastructure.Time;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
