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

            var prompt = $@"
Você é um repórter de notícias em português do Brasil. SUA TAREFA É ENCONTRAR UMA MATÉRIA REAL EM INGLÊS E REESCREVÊ-LA EM PT-BR. 
ATENÇÃO: A NOTÍCIA TEM QUE EXISTIR DE VERDADE. DE FORMA ALGUMA INVENTE FATO, FONTE OU LINK.

Como proceder:
1) Use busca na web (em INGLÊS) para encontrar UMA reportagem RECENTE sobre imigração nos EUA publicada HOJE (preferência) ou nas últimas 24–48 horas. 
   Foque em casos que geram clique de forma responsável: detenções (fronteira/ICE), decisões judiciais que mudam o rumo (green card aprovado após audiência), deportação interrompida, reunificação familiar, acordos, mudanças de política que afetem casos reais etc.
   Use APENAS veículos de notícia (sem fóruns/reddit/blogs pessoais). A matéria tem que existir e ter URL válida.

2) Reescreva a reportagem COMPLETA em PORTUGUÊS DO BRASIL como um texto original jornalístico — 600 a 900 palavras:
   - Tom responsável, informativo e envolvente (narrativa jornalística).
   - Contextualize: cidade/estado, data, órgão (ICE/CBP/USCIS/tribunal), leis/regras citadas, prazos e próximos passos.
   - Inclua aspas/posicionamentos (autoridades, defesa, família) quando houver. 
   - Preserve privacidade quando a fonte não revelar identidade (iniciais/descrições genéricas).
   - NÃO copie trechos longos literalmente; reescreva com suas palavras.

3) Gere UM objeto JSON com as chaves EXATAS:
   - ""Title"" (PT-BR)
   - ""Summary"" (PT-BR)
   - ""Content"" (PT-BR)
   - ""Source""
   - ""Url""
   - ""PublishedAt""

4) Se a matéria for de até 48h atrás, tudo bem, mas informe a data original no corpo; ""PublishedAt"" deve ser agora (UTC).

5) EVITE os títulos já publicados hoje (deduplicação). Se seu melhor título colidir, crie outro igualmente bom.

Títulos publicados hoje:
{string.Join("\n", titulosHoje.Select(t => "- " + t))}

RESPOSTA OBRIGATÓRIA: retorne SOMENTE UM OBJETO JSON COM AS CHAVES {{""Title"",""Summary"",""Content"",""Source"",""Url"",""PublishedAt""}}.
Sem markdown, sem comentários, sem texto extra fora do JSON.
";

            var body = new
            {
                model = "gpt-4o-mini",
                input = prompt,
                tools = new object[] { new { type = "web_search" } },
                max_output_tokens = 4000,
                temperature = 0.6
            };

            var reqContent = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            Console.WriteLine("[API] POST /v1/responses");
            var resp = await http.PostAsync("https://api.openai.com/v1/responses", reqContent);
            var json = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"[API] Status={resp.StatusCode}");
            Console.WriteLine($"[API] Raw(0..600): {(json.Length > 600 ? json[..600] + "..." : json)}");

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine("[API] Chamada sem sucesso; pulando.");
                break;
            }

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

            if (item is null) break;

            if (string.IsNullOrWhiteSpace(item.Url) || !item.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) break;
            if (string.IsNullOrWhiteSpace(item.Source)) break;
            if (string.IsNullOrWhiteSpace(item.Title)) break;
            if (titulosHoje.Contains(item.Title)) break;

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
