using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Services;

public interface IStorageUploader
{
    Task SaveAsync(string site, string localFile, ILogger logger, CancellationToken ct = default);
}