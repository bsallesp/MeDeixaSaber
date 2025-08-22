using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Context;
using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm
{
    internal static partial class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[APP start]");

            const string vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
            var collector = NewsOrgCollector.Create(vaultUrl);

            var payload = await collector.RunAsync("endpoint-newsapi-org-everything");
            if (payload is null)
            {
                Console.WriteLine("[APP warn] payload is null");
                Console.WriteLine("[APP done]");
                return;
            }

            var kv = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
            var openAiKey = (await kv.GetSecretAsync("openai-key")).Value.Value;

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(120);
            var mapper = new NewsMapper();
            var rewriter = new OpenAiNewsRewriter(http, openAiKey, "gpt-4o-mini", verbose: true);
            var journalist = new Journalist(mapper, rewriter);

            const EditorialBias bias = EditorialBias.Neutro;
            var rewritten = await journalist.WriteAsync(payload, bias);

            Console.WriteLine($"[APP] rewritten_count={rewritten.Count}");
            
            var factory = new SqlConnectionFactory(
                "mds-sqlserver-eastus2-prod01.database.windows.net",
                "mds-sql-db-prod");
            var repo = new NewsRepository(factory);

            await repo.InsertManyAsync(rewritten);
            Console.WriteLine($"[APP] persisted_count={rewritten.Count}");

            Console.WriteLine("[APP done]");
        }
    }
}
