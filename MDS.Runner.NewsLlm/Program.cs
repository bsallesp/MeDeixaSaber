using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Context;
using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.NewsLlm;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("[APP start]");

        const string vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
        var secretClient = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
        var secretReader = new SecretReader(secretClient);

        var collector = new NewsOrgCollector(
            secretReader,
            Persisters.BlobSaver.Create("mdsprodstg04512", "news-org"),
            new HttpClient { Timeout = TimeSpan.FromSeconds(60) });

        var openAiKey = await secretReader.GetAsync("openai-key");
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(300);

        var disableRewrite = IsRewriteDisabled();
        IOpenAiNewsRewriter rewriter = disableRewrite
            ? new NoopNewsRewriter()
            : new OpenAiNewsRewriter(http, openAiKey, "gpt-4o-mini", verbose: true);

        var mapper = new NewsMapper();
        var journalist = new Journalist(mapper, rewriter);

        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
        });
        var repoLogger = loggerFactory.CreateLogger<NewsRepository>();

        var factory = new SqlConnectionFactory(
            "mds-sqlserver-eastus2-prod01.database.windows.net",
            "mds-sql-db-prod");
        var repo = new NewsRepository(factory, repoLogger);

        IArticleSink sink = new Application.ArticleSink(repo);
        IAppRunner app = new Application.AppRunner(collector, rewriter, mapper, journalist, sink);

        var count = await app.RunAsync();

        Console.WriteLine($"[APP] persisted_count={count}");
        Console.WriteLine("[APP done]");
    }

    private static bool IsRewriteDisabled()
    {
        var v = Environment.GetEnvironmentVariable("MDS_DISABLE_REWRITE");
        return !string.IsNullOrWhiteSpace(v) && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));
    }
}
