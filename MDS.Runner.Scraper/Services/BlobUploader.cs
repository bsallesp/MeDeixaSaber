using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

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

    public async Task SaveAsync(string site, string localFile, ILogger logger, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(localFile) || !File.Exists(localFile))
        {
            logger.LogWarning("Arquivo local não encontrado ou inválido, pulando upload: {LocalFile}", localFile);
            return;
        }
        
        try
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "ContainerAlreadyExists") { }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao criar o container do blob: {ContainerName}", _container.Name);
            return;
        }

        var blobName = $"{site}/{Path.GetFileName(localFile)}";
        var blob = _container.GetBlobClient(blobName);

        await using var fs = File.OpenRead(localFile);
        try
        {
            await blob.UploadAsync(
                fs,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "text/csv" }
                },
                ct);

            logger.LogInformation("Upload OK para {BlobUri}", blob.Uri);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no upload do arquivo {LocalFile} para {BlobUri}", localFile, blob.Uri);
        }
    }
}