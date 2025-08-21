// using System.Text;
// using Azure.Identity;
// using Azure.Security.KeyVault.Secrets;
// using MeDeixaSaber.Core.Models;
// using MDS.Runner.NewsLlm;
//
// static partial class Program
// {
//     static readonly string[] Topics = { "imigração" };
//
//     static EditorialBias BiasFromArgsOrDefault(string[] args)
//         => args.Length == 0 ? EditorialBias.Neutro :
//            args[0].ToLowerInvariant() switch
//            {
//                "sensacionalista" => EditorialBias.Sensacionalista,
//                "conservador" => EditorialBias.Conservador,
//                "progressista" => EditorialBias.Progressista,
//                "agressivo" => EditorialBias.Agressivo,
//                _ => EditorialBias.Neutro
//            };
//
//     static async Task<string> GetSecretValueAsync(string vaultUrl, string name)
//     {
//         Console.WriteLine($"[KV] {vaultUrl} secret={name}");
//         var client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
//         var s = await client.GetSecretAsync(name);
//         Console.WriteLine($"[KV] ok len={s.Value.Value?.Length}");
//         return s.Value.Value ?? throw new InvalidOperationException("openai-key is empty");
//     }
//
//     static void PrintNews(string header, News n, int preview = 800)
//     {
//         Console.WriteLine($"\n=== {header} ===");
//         Console.WriteLine($"Title: {n.Title}");
//         Console.WriteLine($"Source: {n.Source}");
//         Console.WriteLine($"Url: {n.Url}");
//         Console.WriteLine($"PublishedAt: {n.PublishedAt:O} UTC");
//         if (!string.IsNullOrWhiteSpace(n.Summary)) Console.WriteLine($"\n{n.Summary}");
//         var content = n.Content ?? string.Empty;
//         Console.WriteLine("\n" + (content.Length > preview ? content[..preview] + "...(truncated)" : content));
//         Console.WriteLine("=== END ===\n");
//     }
//
//     static async Task Main(string[] args)
//     {
//         Console.WriteLine("[APP] start");
//         var vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
//         var apiKey = await GetSecretValueAsync(vaultUrl, "openai-key");
//         using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
//
//         var discovery = new NewsLlmCollector(http, apiKey, "gpt-4o-mini", verbose: true);
//         var journalist = new JournalistOld(http, apiKey, "gpt-4o-mini", verbose: true);
//         var bias = BiasFromArgsOrDefault(args);
//
//         var titlesToday = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
//         var topics = args.Length > 0 ? args : Topics;
//
//         foreach (var topic in topics)
//         {
//             Console.WriteLine($"[APP] topic={topic} bias={bias}");
//             var news = await discovery.GetLatestAsync(topic, titlesToday);
//             if (news is null)
//             {
//                 Console.WriteLine("[APP] discovery=null");
//                 continue;
//             }
//             PrintNews("Original", news);
//             try
//             {
//                 var rewritten = await journalist.RewriteAsync(news, bias);
//                 PrintNews($"Reescrita ({bias})", rewritten);
//                 titlesToday.Add(news.Title);
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"[APP] rewrite error: {ex.Message}");
//             }
//         }
//
//         Console.WriteLine("[APP] done");
//     }
// }
