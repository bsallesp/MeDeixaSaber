using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;

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
            if (payload is not null)
            {
                var journalist = new Journalist();
                await journalist.RunAsync(payload);
            }
            else
            {
                Console.WriteLine("[APP warn] payload is null");
            }

            Console.WriteLine("[APP done]");
        }
    }
}