using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MDS.Runner.Scraper.Services;

public sealed class BlobUploader : IStorageUploader
{
    private readonly BlobContainerClient _container;

    public BlobUploader(string accountName, string containerName)
    {
        var uri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}");
        var cred = new DefaultAzureCredential();
        _container = new BlobContainerClient(uri, cred);
    }

    public BlobUploader(string connectionString, string containerName, bool useConnectionString)
    {
        _container = new BlobContainerClient(connectionString, containerName);
    }

    public async Task SaveAsync(string site, string localFile, CancellationToken ct = default)
    {
        try { await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct); }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists") { }

        var blobName = $"{site}/{Path.GetFileName(localFile)}";
        var blob = _container.GetBlobClient(blobName);

        await using var fs = File.OpenRead(localFile);
        await blob.UploadAsync(
            fs,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "text/csv" }
            },
            ct);

        Console.WriteLine($"[BlobUploader] OK: {blob.Uri}");
    }
}