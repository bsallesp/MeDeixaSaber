using System.Text;
using System.Text.Json;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public enum EditorialBias
    {
        Neutro,
        Sensacionalista,
        Conservador,
        Progressista,
        Agressivo
    }

    public interface IJournalistOld
    {
        Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default);
    }

    public sealed class JournalistOld : IJournalistOld
    {
        readonly HttpClient _http;
        readonly string _apiKey;
        readonly string _model;
        readonly bool _verbose;

        public JournalistOld(HttpClient http, string apiKey, string model = "gpt-4o-mini", bool verbose = false)
        {
            _http = http;
            _apiKey = apiKey;
            _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
            _verbose = verbose;
        }

        public async Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default)
        {
            var biasInstr = bias switch
            {
                EditorialBias.Sensacionalista => "ênfase em impacto, títulos fortes, ritmo acelerado",
                EditorialBias.Conservador => "tom sóbrio, foco em lei e ordem, responsabilidade fiscal",
                EditorialBias.Progressista => "tom humanizado, foco em direitos, impactos sociais",
                EditorialBias.Agressivo => "tom direto, crítico, frases curtas",
                _ => "equilíbrio e neutralidade"
            };

            var prompt = $@"
Reescreva em pt-BR com viés: {biasInstr}.
Título conciso, lide claro, intertítulos e contexto. 600-900 palavras.
Mantenha Source, Url e PublishedAt exatamente iguais.
Responda apenas JSON válido.
Dados:
Title: {source.Title}
Source: {source.Source}
Url: {source.Url}
PublishedAt: {source.PublishedAt:yyyy-MM-ddTHH:mm:ssZ}
Resumo: {source.Summary}
Conteúdo: {source.Content}

Formato:
{{
  ""Title"": ""..."",
  ""Summary"": ""..."",
  ""Content"": ""..."",
  ""Source"": ""{source.Source}"",
  ""Url"": ""{source.Url}"",
  ""PublishedAt"": ""{source.PublishedAt:yyyy-MM-ddTHH:mm:ssZ}"",
  ""CreatedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
}}";

            var body = new
            {
                model = _model,
                input = prompt,
                temperature = 0.6,
                max_output_tokens = 2000,
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "news",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            required = new[]
                                { "Title", "Summary", "Content", "Source", "Url", "PublishedAt", "CreatedAt" },
                            properties = new
                            {
                                Title = new { type = "string", minLength = 5, maxLength = 300 },
                                Summary = new { type = "string", minLength = 10, maxLength = 400 },
                                Content = new { type = "string", minLength = 300 },
                                Source = new { type = "string", minLength = 2, maxLength = 120 },
                                Url = new { type = "string", minLength = 10, maxLength = 600 },
                                PublishedAt = new { type = "string" },
                                CreatedAt = new { type = "string" }
                            },
                            additionalProperties = false
                        }
                    }
                }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
            {
                Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey) },
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            if (_verbose) Console.WriteLine($"[JOUR] model={_model} bias={bias} promptLen={prompt.Length}");

            using var resp = await _http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (_verbose) Console.WriteLine($"[JOUR] status={(int)resp.StatusCode} bodyLen={json.Length}");

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Rewrite failed: {(int)resp.StatusCode} {json}");

            var payload = ExtractTextPayload(json) ?? TrySliceFirstJsonObject(json);
            if (_verbose) Console.WriteLine($"[JOUR] payloadLen={(payload?.Length ?? 0)}");

            if (string.IsNullOrWhiteSpace(payload)) throw new InvalidOperationException("Empty rewrite payload");

            var rewritten = DeserializeToNews(payload) ?? throw new InvalidOperationException("Invalid rewrite JSON");

            rewritten.Source = source.Source;
            rewritten.Url = source.Url;
            rewritten.PublishedAt = source.PublishedAt == default ? DateTime.UtcNow : source.PublishedAt;
            if (rewritten.CreatedAt == default) rewritten.CreatedAt = DateTime.UtcNow;

            if (_verbose) Console.WriteLine("[JOUR] ok");
            return rewritten;
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
            var source = GetStr("Source", "source");
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