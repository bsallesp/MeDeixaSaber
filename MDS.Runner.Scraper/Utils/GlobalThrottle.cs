using System;
using System.Threading;
using System.Threading.Tasks;

namespace MDS.Runner.Scraper.Utils;

public static class GlobalThrottle
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTimeOffset _nextAllowed = DateTimeOffset.MinValue;

    public static async Task WaitAsync(int pauseMs, int jitterMs)
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (_nextAllowed > now)
            {
                var delay = _nextAllowed - now;
                if (delay > TimeSpan.Zero) await Task.Delay(delay).ConfigureAwait(false);
            }

            var j = jitterMs > 0 ? Random.Shared.Next(-jitterMs, jitterMs + 1) : 0;
            var total = Math.Max(0, pauseMs + j);
            _nextAllowed = DateTimeOffset.UtcNow.AddMilliseconds(total);
        }
        finally
        {
            Gate.Release();
        }
    }
}