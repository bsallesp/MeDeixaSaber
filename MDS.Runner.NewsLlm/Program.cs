using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Data;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;

static class Program
{
    static readonly string[] Topics = { "imigração" };

    static async Task<string> GetSecretValueAsync(string vaultUrl, string name)
    {
        Console.WriteLine($"[KV] Obtendo secret '{name}'...");
        var client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
        var s = await client.GetSecretAsync(name);
        Console.WriteLine($"[KV] OK '{name}' (len={s.Value.Value.Length}).");
        return s.Value.Value;
    }

    static string? ExtractTextPayload(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
                return ot.GetString();
            if (doc.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
            {
                foreach (var msg in output.EnumerateArray())
                {
                    if (!msg.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
                    foreach (var part in content.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var txt))
                        {
                            if (txt.ValueKind == JsonValueKind.String) return txt.GetString();
                            if (txt.ValueKind == JsonValueKind.Object && txt.TryGetProperty("value", out var val) && val.ValueKind == JsonValueKind.String)
                                return val.GetString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] ExtractTextPayload: {ex.Message}");
        }
        return null;
    }

    static async Task<string> PostWithRetryAsync(HttpClient http, object body, int max = 5)
    {
        for (var i = 1; i <= max; i++)
        {
            var req = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var resp = await http.PostAsync("https://api.openai.com/v1/responses", req);
            var txt = await resp.Content.ReadAsStringAsync();
            if ((int)resp.StatusCode < 500 && resp.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
            {
                if (!resp.IsSuccessStatusCode) throw new Exception($"Bad status {(int)resp.StatusCode}: {txt}");
                return txt;
            }
            var backoff = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, i) + Random.Shared.NextDouble()));
            await Task.Delay(backoff);
        }
        throw new Exception("Max retries reached.");
    }

    static async Task<bool> UrlOkAsync(string url)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if ((int)resp.StatusCode == 200) return true;
            using var get = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp2 = await http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead);
            return (int)resp2.StatusCode == 200;
        }
        catch { return false; }
    }

    static async Task Main(string[] args)
    {
        var vaultUrl = "https://web-app-vault-sql.vault.azure.net/";
        var apiKey = await GetSecretValueAsync(vaultUrl, "openai-key");

        var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "tcp:mds-sqlserver-eastus2-prod01.database.windows.net,1433";
        var database = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "mds-sql-db-prod";
        Console.WriteLine($"[DB] Server={server}");
        Console.WriteLine($"[DB] Database={database}");

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var topics = args.Length > 0 ? args : Topics;

        var factory = new SqlConnectionFactory(server, database);
        var repo = new NewsRepository(factory);

        var hojeUtc = DateTime.UtcNow.Date;
        var titulosHoje = (await repo.GetTitlesByDayAsync(hojeUtc)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Console.WriteLine($"[DEDUP] Títulos hoje: {titulosHoje.Count}");

        foreach (var topic in topics)
        {
            Console.WriteLine($"\n=== Gerando notícia para: {topic} ===");

            var raw = await GetSecretValueAsync(vaultUrl, "kv-prompt-news");
            var prompt = raw
                .Replace(@"\_", "_").Replace(@"\:", ":")
                .Replace("{TOPIC}", topic)
                .Replace("{TODAY_UTC}", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                .Replace("{TITULOS_HOJE}", string.Join("\n", titulosHoje.Select(t => "- " + t)));

            var body = new
            {
                model = "gpt-4o-mini",
                input = prompt,
                tools = new object[] { new { type = "web_search" } },
                tool_choice = "auto",
                temperature = 0.4,
                max_output_tokens = 1600
            };

            Console.WriteLine("[API] POST /v1/responses");
            string json;
            try
            {
                json = await PostWithRetryAsync(http, body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Falha: {ex.Message}");
                break;
            }

            Console.WriteLine($"[API] Raw(0..600): {(json.Length > 600 ? json[..600] + "..." : json)}");

            var payload = ExtractTextPayload(json);
            if (string.IsNullOrWhiteSpace(payload))
            {
                var s = json.IndexOf('{');
                var e = json.LastIndexOf('}');
                payload = (s >= 0 && e > s) ? json.Substring(s, e - s + 1) : null;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine("[WARN] Sem payload de texto; pulando.");
                break;
            }

            Console.WriteLine($"[API] Payload len={payload.Length}");

            News? item = null;

            try
            {
                item = JsonSerializer.Deserialize<News>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<List<News>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (arr is { Count: > 0 }) item = arr[0];
                }
                catch { }
            }

            if (item is null)
            {
                await File.WriteAllTextAsync("payload.txt", payload);
                Console.WriteLine("[DEBUG] Payload salvo em payload.txt");
                break;
            }

            if (string.IsNullOrWhiteSpace(item.Url) || !item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) break;
            if (string.IsNullOrWhiteSpace(item.Source)) break;
            if (string.IsNullOrWhiteSpace(item.Title)) break;
            if (titulosHoje.Contains(item.Title)) break;

            if (!await UrlOkAsync(item.Url)) break;

            if (item.PublishedAt == default) item.PublishedAt = DateTime.UtcNow;
            if (item.CreatedAt == default) item.CreatedAt = DateTime.UtcNow;

            Console.WriteLine($"[OK] Notícia gerada: {item.Title}");
            Console.WriteLine($"[INFO] Fonte: {item.Source} | Url: {item.Url}");
            await repo.InsertAsync(item);
            Console.WriteLine("[DONE] Salvo com sucesso.");
            break;
        }

        Console.WriteLine("[FIM] Execução concluída.");
    }
}
