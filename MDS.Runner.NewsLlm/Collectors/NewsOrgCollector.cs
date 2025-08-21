using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Security.KeyVault.Secrets;
using MDS.Runner.NewsLlm.Persisters;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Collectors
{
    public interface INewsOrgCollector
    {
        Task<NewsApiResponse?> RunAsync(string secretName, CancellationToken ct = default);
    }

    public sealed class NewsOrgCollector(SecretClient secrets, IBlobSaver blobSaver) : INewsOrgCollector
    {
        readonly SecretClient _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        readonly IBlobSaver _blobSaver = blobSaver ?? throw new ArgumentNullException(nameof(blobSaver));

        public async Task<NewsApiResponse?> RunAsync(string secretName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(secretName)) throw new ArgumentException("Required", nameof(secretName));

            var runId = Guid.NewGuid().ToString("N");
            Console.WriteLine($"[RUN start] id={runId} secretName={secretName}");

            var totalSw = Stopwatch.StartNew();

            var kvSw = Stopwatch.StartNew();
            var endpoint = (await _secrets.GetSecretAsync(secretName, cancellationToken: ct)).Value.Value ?? string.Empty;
            kvSw.Stop();
            Console.WriteLine($"[KV ok] id={runId} len={endpoint.Length} ms={kvSw.ElapsedMilliseconds}");

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(60);

            using var req = new HttpRequestMessage(HttpMethod.Get, endpoint);
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("MDS.Runner.NewsLlm", "1.0"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var httpSw = Stopwatch.StartNew();
            using var resp = await http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            httpSw.Stop();

            var preview = body.Length > 300 ? body.Substring(0, 300) + "...(truncated)" : body;
            Console.WriteLine($"[HTTP] id={runId} status={(int)resp.StatusCode} ms={httpSw.ElapsedMilliseconds} bytes={Encoding.UTF8.GetByteCount(body)}");
            Console.WriteLine($"[HTTP] id={runId} preview={preview}");

            var prefix = resp.IsSuccessStatusCode ? "newsapi-everything" : $"newsapi-everything-error-{(int)resp.StatusCode}";
            var blobSw = Stopwatch.StartNew();
            var uri = await _blobSaver.SaveJsonAsync(body, prefix, ct);
            blobSw.Stop();
            Console.WriteLine($"[BLOB ok] id={runId} uri={uri} ms={blobSw.ElapsedMilliseconds}");

            NewsApiResponse? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<NewsApiResponse>(
                    body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"[JSON ok] id={runId} articles={(payload?.Articles?.Count ?? 0)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JSON error] id={runId} msg={ex.Message}");
            }

            totalSw.Stop();
            Console.WriteLine($"[RUN done] id={runId} total_ms={totalSw.ElapsedMilliseconds}");

            return payload;
        }

        public static NewsOrgCollector Create(string vaultUrl)
        {
            var secrets = new SecretClient(new Uri(vaultUrl), new Azure.Identity.DefaultAzureCredential());
            var saver = BlobSaver.Create("mdsprodstg04512", "news-org");
            return new NewsOrgCollector(secrets, saver);
        }
    }
}
