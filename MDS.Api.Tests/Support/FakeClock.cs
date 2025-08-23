using MDS.Application.Time;

namespace MDS.Api.Tests.Support;

public sealed class FakeClock(DateTime? fixedNow = null) : IClock
{
    private readonly DateTime _now = (fixedNow ?? DateTime.UtcNow).AddMinutes(-1);

    public DateTime UtcNow => _now;
}