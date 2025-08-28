using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MDS.Runner.Scraper.Services;

public sealed class LocalUploader : IStorageUploader
{
    private readonly string? _baseDir;

    public LocalUploader()
    {
        _baseDir = null;
    }

    public LocalUploader(string baseDir)
    {
        _baseDir = string.IsNullOrWhiteSpace(baseDir) ? null : baseDir;
    }

    public Task SaveAsync(string site, string localFile, CancellationToken ct = default)
        => SaveAsync(site, localFile, NullLogger.Instance, ct);

    public Task SaveAsync(string site, string localFile, ILogger logger, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(localFile) || !File.Exists(localFile))
        {
            logger.LogWarning("Arquivo local não encontrado ou inválido, pulando cópia local: {LocalFile}", localFile);
            return Task.CompletedTask;
        }

        string destDir;
        if (!string.IsNullOrWhiteSpace(_baseDir))
        {
            Directory.CreateDirectory(_baseDir!);
            destDir = Path.Combine(_baseDir!, site);
        }
        else
        {
            var root = Path.GetDirectoryName(localFile)!;
            destDir = Path.Combine(root, "uploaded", site);
        }

        Directory.CreateDirectory(destDir);

        var destFile = Path.Combine(destDir, Path.GetFileName(localFile));
        File.Copy(localFile, destFile, overwrite: true);

        logger.LogInformation("[LocalUploader] Copiado para: {DestinationFile}", destFile);
        return Task.CompletedTask;
    }
}