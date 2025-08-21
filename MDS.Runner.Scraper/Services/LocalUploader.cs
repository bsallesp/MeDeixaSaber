namespace MDS.Runner.Scraper.Services;

public sealed class LocalUploader(string? baseDir = null) : IStorageUploader
{
    readonly string _baseDir = string.IsNullOrWhiteSpace(baseDir)
        ? Path.Combine(Directory.GetCurrentDirectory(), "scraped")
        : baseDir;

    public async Task SaveAsync(string source, string path, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(path);
        var destDir = Path.Combine(_baseDir, source);
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, fileName);

        await using var src = File.OpenRead(path);
        await using var dst = File.Create(destPath);
        await src.CopyToAsync(dst, cancellationToken);
    }
}