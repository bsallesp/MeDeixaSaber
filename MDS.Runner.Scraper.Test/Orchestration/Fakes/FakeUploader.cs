using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MDS.Runner.Scraper.Services;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Test.Orchestration;

public sealed class FakeUploader : IStorageUploader
{
    public readonly List<(string src, string file, ILogger logger, CancellationToken ct)> Saves = new();
    public Task SaveAsync(string site, string localFile, ILogger logger, CancellationToken ct = default)
    {
        Saves.Add((site, localFile, logger, ct));
        return Task.CompletedTask;
    }
}