using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Context;
using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MDS.Runner.NewsLlm.Persisters;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm
{
    internal interface IArticleSink
    {
        Task InsertManyAsync(IEnumerable<News> items);
    }

    internal sealed class ArticleSink(NewsRepository repo) : IArticleSink
    {
        public Task InsertManyAsync(IEnumerable<News> items) => repo.InsertManyAsync(items);
    }

    internal interface IAppRunner
    {
        Task<int> RunAsync(CancellationToken ct = default);
    }

    internal sealed class AppRunner(
        INewsOrgCollector collector,
        IOpenAiNewsRewriter rewriter,
        INewsMapper mapper,
        IJournalist journalist,
        IArticleSink sink) : IAppRunner
    {
        public async Task<int> RunAsync(CancellationToken ct = default)
        {
            var payload = await collector.RunAsync("endpoint-newsapi-org-everything", ct);
            if (payload is null) return 0;

            var rewritten = await journalist.WriteAsync(payload, EditorialBias.Neutro, ct);
            var count = 0;

            foreach (var item in rewritten.Take(30))
            {
                try
                {
                    await sink.InsertManyAsync([item]);
                    count++;
                }
                catch
                {
                    // ignored
                }
            }

            return count;
        }
    }

    internal static partial class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[APP start]");

            const string vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
            var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
            var secretReader = new SecretReader(secretClient);

            var collector = new NewsOrgCollector(
                secretReader,
                BlobSaver.Create("mdsprodstg04512", "news-org"),
                new HttpClient { Timeout = TimeSpan.FromSeconds(60) });

            var openAiKey = await secretReader.GetAsync("openai-key");
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
            var rewriter = new OpenAiNewsRewriter(http, openAiKey, "gpt-4o-mini", verbose: true);

            var mapper = new NewsMapper();
            var journalist = new Journalist(mapper, rewriter);

            var factory = new SqlConnectionFactory(
                "mds-sqlserver-eastus2-prod01.database.windows.net",
                "mds-sql-db-prod");
            var repo = new NewsRepository(factory);
            var sink = new ArticleSink(repo);

            var app = new AppRunner(collector, rewriter, mapper, journalist, sink);
            var count = await app.RunAsync();

            Console.WriteLine($"[APP] persisted_count={count}");
            Console.WriteLine("[APP done]");
        }
    }
}
