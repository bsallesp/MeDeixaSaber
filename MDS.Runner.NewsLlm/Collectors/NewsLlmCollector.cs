using System.Text;
using System.Text.Json;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Collectors
{
    public interface INewsCollectorService
    {
        Task<News?> GetLatestAsync(string topic, ISet<string> titlesToday, CancellationToken ct = default);
    }

    public sealed class NewsLlmCollector(
        HttpClient http,
        string openAiApiKey,
        string model = "gpt-4o-mini",
        bool verbose = false)
        : INewsCollectorService
    {
        readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;

        public async Task<News?> GetLatestAsync(string topic, ISet<string> titlesToday, CancellationToken ct = default)
        {
            topic ??= "imigração";
            titlesToday ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var prompt = $@"
                Do a web scrap and Bring ONE real and TODAY news article an american journal about {topic} in the United States.
                Use web_search to find it and ensure the final URL resolves with status 200.
                Return ONLY a STRICT JSON OBJECT (no markdown, no commentary, no code fences):

                {{
                  ""Title"": ""..."",
                  ""Summary"": ""..."",
                  ""Content"": ""..."",
                  ""Source"": ""..."",
                  ""Url"": ""https://..."",
                  ""PublishedAt"": ""YYYY-MM-DDTHH:mm:ssZ"",
                  ""CreatedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}

                Language: pt-BR. Content must be 600–900 words, journalistic tone. 
                ";

            Console.WriteLine("----------------------------");
            Console.WriteLine(prompt);
            Console.WriteLine("----------------------------");

            var body = new
            {
                model = _model,
                input = prompt,
                tools = new object[] { new { type = "web_search" } },
                tool_choice = "auto",
                temperature = 0.4,
                max_output_tokens = 1600
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiApiKey) },
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            if (verbose) Console.WriteLine($"[DISC] model={_model} topic={topic} promptLen={prompt.Length}");

            using var resp = await http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            
            if (verbose) Console.WriteLine($"[DISC] status={(int)resp.StatusCode} bodyLen={json.Length}");

            if (!resp.IsSuccessStatusCode)
            {
                if (verbose) Console.WriteLine($"[DISC] errorBody={json}");
                return null;
            }

            var payload = ExtractTextPayload(json) ?? TrySliceFirstJsonObject(json);
            if (verbose) Console.WriteLine($"[DISC] payloadFirst200={(payload is null ? "" : payload[..Math.Min(payload.Length,200)])}");

            if (string.IsNullOrWhiteSpace(payload)) return null;

            var news = DeserializeToNews(payload);
            if (news is null)
            {
                if (verbose) Console.WriteLine("[DISC] invalid JSON for News");
                return null;
            }

            if (string.IsNullOrWhiteSpace(news.Title) ||
                string.IsNullOrWhiteSpace(news.Source) ||
                string.IsNullOrWhiteSpace(news.Url) ||
                !news.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if (verbose) Console.WriteLine("[DISC] missing required fields");
                return null;
            }

            if (titlesToday.Contains(news.Title))
            {
                if (verbose) Console.WriteLine("[DISC] duplicate title skipped");
                return null;
            }

            if (news.PublishedAt == default) news.PublishedAt = DateTime.UtcNow;
            if (news.CreatedAt == default) news.CreatedAt = DateTime.UtcNow;

            if (verbose) Console.WriteLine($"[DISC] ok title='{news.Title}' source='{news.Source}'");
            return news;
        }

        static string? ExtractTextPayload(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
                    return ot.GetString();
                if (doc.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                {
                    foreach (var msg in output.EnumerateArray())
                    {
                        if (!msg.TryGetProperty("content", out var content) ||
                            content.ValueKind != JsonValueKind.Array) continue;
                        foreach (var part in content.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var txt))
                            {
                                if (txt.ValueKind == JsonValueKind.String) return txt.GetString();
                                if (txt.ValueKind == JsonValueKind.Object &&
                                    txt.TryGetProperty("value", out var val) &&
                                    val.ValueKind == JsonValueKind.String) return val.GetString();
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        static string? TrySliceFirstJsonObject(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            return (start >= 0 && end > start) ? s.Substring(start, end - start + 1) : null;
        }

        static News? DeserializeToNews(string payload)
        {
            if (TryDeserialize<News>(payload, out var one) && one is not null) return one;
            if (TryDeserialize<List<News>>(payload, out var list) && list is { Count: > 0 }) return list[0];
            if (TryDeserialize<JsonElement>(payload, out var el) && el.ValueKind == JsonValueKind.Object)
                return MapLooseObject(el);
            return null;
        }

        static bool TryDeserialize<T>(string json, out T? value)
        {
            try
            {
                value = JsonSerializer.Deserialize<T>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return value is not null;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        static News? MapLooseObject(JsonElement obj)
        {
            string GetStr(params string[] keys)
            {
                foreach (var k in keys)
                    if (obj.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString()!;
                return string.Empty;
            }

            var title = GetStr("Title", "title");
            var source = GetStr("Source", "source", "Outlet", "outlet");
            var url = GetStr("Url", "url", "Link", "link");
            var summary = GetStr("Summary", "summary");
            var content = GetStr("Content", "content", "Body", "body");
            var publishedAtStr = GetStr("PublishedAt", "publishedAt");
            var createdAtStr = GetStr("CreatedAt", "createdAt");

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(url))
                return null;

            var publishedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(publishedAtStr) && DateTime.TryParse(publishedAtStr, out var p))
                publishedAt = p.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(p, DateTimeKind.Utc)
                    : p.ToUniversalTime();

            var createdAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(createdAtStr) && DateTime.TryParse(createdAtStr, out var c))
                createdAt = c.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(c, DateTimeKind.Utc)
                    : c.ToUniversalTime();

            return new News
            {
                Title = title,
                Source = source,
                Url = url,
                Summary = string.IsNullOrWhiteSpace(summary) ? null : summary,
                Content = string.IsNullOrWhiteSpace(content) ? "(sem conteúdo)" : content,
                PublishedAt = publishedAt,
                CreatedAt = createdAt
            };
        }
    }
}