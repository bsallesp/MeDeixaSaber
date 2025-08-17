using Azure.Identity;
using Azure.Storage.Blobs;

namespace MDS.Scraper.Services;

public class BlobUploader : IStorageUploader
{
    private readonly BlobContainerClient _container;

    public BlobUploader(string accountName, string containerName)
    {
        var uri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        _container = new BlobContainerClient(uri, new DefaultAzureCredential());
    }

    public async Task SaveAsync(string site, string localFile)
    {
        var blobName = $"{site}/date={DateTime.UtcNow:yyyy-MM-dd}/{Path.GetFileName(localFile)}";

        if (!File.Exists(localFile))
        {
            Console.WriteLine($"[BlobUploader] ERRO: arquivo não encontrado: {localFile}");
            return;
        }

        var length = new FileInfo(localFile).Length;
        Console.WriteLine($"[BlobUploader] Preparando upload: {localFile} ({length} bytes)");

        await using var stream = File.OpenRead(localFile);
        await _container.UploadBlobAsync(blobName, stream);

        Console.WriteLine($"[BlobUploader] OK: {blobName} enviado com {length} bytes");
    }
}