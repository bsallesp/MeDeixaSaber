namespace MDS.Runner.Scraper.Services;

public interface IStorageUploader
{
    Task SaveAsync(string site, string localFile, CancellationToken ct = default);
}