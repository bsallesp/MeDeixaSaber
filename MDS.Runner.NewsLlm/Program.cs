using System.Text;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MDS.Data.Data;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;

static class Program
{
    static readonly string[] Topics = { "immigration", "employment", "economy", "politics" };

    static async Task<string> GetSecretValueAsync(string vaultUrl, string name)
    {
        Console.WriteLine($"[KV] Getting secret '{name}'...");
        var client = new SecretClient(new Uri(vaultUrl), new DefaultAzureCredential());
        var s = await client.GetSecretAsync(name);
        Console.WriteLine($"[KV] Got '{name}' (len={s.Value.Value.Length}).");
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
                        if (part.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "output_text")
                        {
                            if (part.TryGetProperty("text", out var txt2) && txt2.ValueKind == JsonValueKind.String) return txt2.GetString();
                            if (part.TryGetProperty("text", out var txtObj) && txtObj.ValueKind == JsonValueKind.Object && txtObj.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.String)
                                return v.GetString();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERR] ExtractTextPayload: {ex.Message}");
        }
        return null;
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

        foreach (var topic in topics)
        {
            Console.WriteLine($"\n=== Topic: {topic} ===");

            var input = $@"Return ONLY a JSON array. No prose, no markdown.
Each item: Title, Summary, Content, Source, Url, PublishedAt (ISO 8601 UTC).
Exactly 5 items about ""{topic}"".";

            var body = new
            {
                model = "gpt-4o-mini",
                input,
                max_output_tokens = 2000,
                temperature = 0.4
            };

            var reqContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            Console.WriteLine("[API] POST /v1/responses");
            var resp = await http.PostAsync("https://api.openai.com/v1/responses", reqContent);
            var json = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[API] Status={resp.StatusCode}");
            Console.WriteLine($"[API] Raw(0..500): {(json.Length > 500 ? json[..500] + "..." : json)}");

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine("[API] Non-success. Skipping topic.");
                continue;
            }

            var payload = ExtractTextPayload(json);
            if (string.IsNullOrWhiteSpace(payload))
            {
                var start = json.IndexOf('[');
                var end = json.LastIndexOf(']');
                payload = (start >= 0 && end > start) ? json.Substring(start, end - start + 1) : null;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine("[WARN] No text payload. Skipping.");
                continue;
            }

            Console.WriteLine($"[API] Payload len={payload.Length}");
            List<News>? items = null;

            try
            {
                items = JsonSerializer.Deserialize<List<News>>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Parse fail: {ex.Message}");
                var start = payload.IndexOf('[');
                var end = payload.LastIndexOf(']');
                if (start >= 0 && end > start)
                {
                    var slice = payload.Substring(start, end - start + 1);
                    Console.WriteLine($"[INFO] Retrying slice len={slice.Length}");
                    try
                    {
                        items = JsonSerializer.Deserialize<List<News>>(slice, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"[ERR] Retry parse fail: {ex2.Message}");
                    }
                }
            }

            if (items is null || items.Count == 0)
            {
                Console.WriteLine("[WARN] No items parsed.");
                continue;
            }

            Console.WriteLine($"[OK] Parsed {items.Count} items. Upserting...");
            await repo.InsertManyAsync(items);
            foreach (var n in items)
                Console.WriteLine($"[DB] Upsert: {n.Title} | {n.Source} | {n.Url}");
            Console.WriteLine("[DONE] Batch saved.");
        }
    }
}
