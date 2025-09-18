using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Context;
using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Application;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Integrations.Gemini;
using MDS.Runner.NewsLlm.Journalists.Gemini;
using MDS.Runner.NewsLlm.Persisters;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.NewsLlm
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("[APP start]");

            const string vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
            var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
            var secretReader = new SecretReader(secretClient);

            var geminiApiKey = await secretReader.GetAsync("gemini-api");
            var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

            var geminiService = new GeminiService(http, geminiApiKey);
            var geminiJournalist = new GeminiJournalistService(geminiService);

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            });
            var repoLogger = loggerFactory.CreateLogger<NewsRepository>();

            var factory = new SqlConnectionFactory(
                "mds-sqlserver-eastus2-prod01.database.windows.net",
                "mds-sql-db-prod");

            var repo = new NewsRepository(factory, repoLogger);
            var reader = new DbArticleRead(repo);
            var dbSink = new DbArticleSink(repo);
            var blob = BlobSaver.Create("mdsprodstg04512", "news-llm");
            var blobSink = new BlobArticleSink(blob, "news-llm");
            IArticleSink sink = new CompositeArticleSink([dbSink, blobSink]);

            IImageFinder imageFinder = new UnsplashImageFinder(http, secretReader);

            IAppRunner app = new AppRunner(geminiJournalist, sink, reader, imageFinder);

            var count = await app.RunAsync();

            Console.WriteLine($"[APP] persisted_count={count}");
            Console.WriteLine("[APP done]");
        }
    }
}