using System.Diagnostics;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Test.Utils;

public class GlobalThrottleTests
{
    [Fact]
    public async Task Enforces_MinimumDelay()
    {
        var sw = Stopwatch.StartNew();
        await GlobalThrottle.WaitAsync(120, 0);
        await GlobalThrottle.WaitAsync(120, 0);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 120);
    }

    [Fact]
    public async Task Applies_Jitter()
    {
        var results = new long[6];
        for (int i = 0; i < results.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            await GlobalThrottle.WaitAsync(60, 30);
            sw.Stop();
            results[i] = sw.ElapsedMilliseconds;
        }
        Assert.Contains(results, r => r < 80);
        Assert.Contains(results, r => r > 60);
    }

    [Fact]
    public async Task Works_With_Concurrency()
    {
        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, 5).Select(_ => GlobalThrottle.WaitAsync(50, 0));
        await Task.WhenAll(tasks);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 250);
    }
}