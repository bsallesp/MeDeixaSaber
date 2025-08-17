namespace MDS.Scraper.Services;

public class LocalUploader : IStorageUploader
{
    public async Task SaveAsync(string site, string localFile, CancellationToken ct = default)
    {
        var dir = Path.Combine("test-output", site);
        Directory.CreateDirectory(dir);
        var destFile = Path.Combine(dir, Path.GetFileName(localFile));

        await using var source = File.OpenRead(localFile);
        await using var dest = File.Create(destFile);
        await source.CopyToAsync(dest, ct);

        Console.WriteLine($"[LocalUploader] saved {destFile}");
    }
}