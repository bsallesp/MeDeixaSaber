using System.Diagnostics;
using System.Text;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace MDS.Runner.NewsLlm.Persisters
{
    public interface IBlobSaver
    {
        Task<Uri> SaveJsonAsync(string json, string filePrefix, CancellationToken ct = default);
    }

    public sealed class BlobSaver(BlobContainerClient container) : IBlobSaver
    {
        readonly BlobContainerClient _container = container ?? throw new ArgumentNullException(nameof(container));

        public static BlobSaver Create(string account, string container)
        {
            if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("Required", nameof(account));
            if (string.IsNullOrWhiteSpace(container)) throw new ArgumentException("Required", nameof(container));
            var uri = new Uri($"https://{account}.blob.core.windows.net/{container}");
            var client = new BlobContainerClient(uri, new DefaultAzureCredential());
            return new BlobSaver(client);
        }

        public async Task<Uri> SaveJsonAsync(string json, string filePrefix, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Required", nameof(json));
            if (string.IsNullOrWhiteSpace(filePrefix)) throw new ArgumentException("Required", nameof(filePrefix));

            var ensureSw = Stopwatch.StartNew();
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
            ensureSw.Stop();
            Console.WriteLine($"[BLOB ensure] container={_container.Name} ms={ensureSw.ElapsedMilliseconds}");

            var name = $"dt={DateTime.UtcNow:yyyy-MM-dd}/{filePrefix}-{DateTime.UtcNow:HHmmssfff}-{Guid.NewGuid():N}.json";
            var blob = _container.GetBlobClient(name);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var headers = new BlobHttpHeaders { ContentType = "application/json; charset=utf-8" };

            var uploadSw = Stopwatch.StartNew();
            await blob.UploadAsync(ms, new BlobUploadOptions { HttpHeaders = headers }, ct);
            uploadSw.Stop();
            Console.WriteLine($"[BLOB upload] name={name} size_bytes={ms.Length} ms={uploadSw.ElapsedMilliseconds}");

            return blob.Uri;
        }
    }
}
