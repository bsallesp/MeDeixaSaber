using System.Text;
using System.Text.Json;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public interface IOpenAiNewsRewriter
    {
        Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default);
    }

    public sealed class OpenAiNewsRewriter(
        HttpClient http,
        string apiKey,
        string model = "gpt-4o-mini",
        bool verbose = false)
        : IOpenAiNewsRewriter
    {
        private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
        private readonly string _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentException("Required", nameof(apiKey)) : apiKey;
        private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;

        private const int MaxContentChars = 1500;
        private const int MaxOutputTokens = 900;
        private const int MaxRetries = 3;
        private static readonly TimeSpan PerAttemptTimeout = TimeSpan.FromSeconds(20);

        public async Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            var biasInstr = bias switch
            {
                EditorialBias.Sensacionalista => "ritmo acelerado, verbos fortes, sem exageros factuais, zero caixa-alta",
                EditorialBias.Conservador     => "tom sóbrio, ênfase em lei e ordem, custos fiscais e segurança pública",
                EditorialBias.Progressista    => "tom humanizado, foco em direitos, impacto social e equidade",
                EditorialBias.Agressivo       => "frases curtas, direto ao ponto, cobre inconsistências e responsabiliza",
                _                              => "crítico e equilibrado; descreva evidências; evite adjetivos normativos"
            };

            var desc = source.Summary ?? string.Empty;
            var content = source.Content ?? string.Empty;
            if (content.Length > MaxContentChars) content = content[..MaxContentChars];

            var prompt = $@"Reescreva a notícia abaixo em português (pt-BR), {biasInstr}.
                Produza 520–700 palavras, sem inventar fatos ou números.
                Mantenha:
                - Source: ""{source.Source}""
                - Url: ""{source.Url}""
                - PublishedAt: ""{source.PublishedAt:yyyy-MM-ddTHH:mm:ssZ}""
                Regras:
                - Title: 52–70 caracteres.
                - Summary: 1 frase (≤220).
                - Content: Lide; ""Contexto""; ""O que está em jogo""; ""O que dizem as partes""; ""Próximos passos""; fecho.
                Proibições: nada inventado.
                Dados:
                Título original: {source.Title}
                Resumo original: {desc}
                Trecho do conteúdo:
                {content}
                Responda APENAS com JSON:
                {{
                  ""Title"": ""..."",
                  ""Summary"": ""..."",
                  ""Content"": ""..."",
                  ""Source"": ""{source.Source}"",
                  ""Url"": ""{source.Url}"",
                  ""PublishedAt"": ""{source.PublishedAt:yyyy-MM-ddTHH:mm:ssZ}"",
                  ""CreatedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}""
                }}";

            var schema = new
            {
                type = "object",
                additionalProperties = false,
                required = new[] { "Title", "Summary", "Content", "Source", "Url", "PublishedAt", "CreatedAt" },
                properties = new
                {
                    Title = new { type = "string" },
                    Summary = new { type = "string" },
                    Content = new { type = "string" },
                    Source = new { type = "string", const_ = source.Source },
                    Url = new { type = "string", const_ = source.Url },
                    PublishedAt = new { type = "string", const_ = source.PublishedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") },
                    CreatedAt = new { type = "string" }
                }
            };

            var body = new
            {
                model = _model,
                input = prompt,
                temperature = 0.5,
                max_output_tokens = MaxOutputTokens,
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "news",
                        strict = true,
                        schema
                    }
                }
            };

            for (var attempt = 0; attempt < MaxRetries; attempt++)
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = new StringContent(JsonSerializer.Serialize(body).Replace("\"const_\"", "\"const\""), Encoding.UTF8, "application/json");

                if (verbose) Console.WriteLine($"[JOUR request] model={_model} bias={bias} title={source.Title} attempt={attempt + 1}");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(PerAttemptTimeout);

                string json;
                HttpResponseMessage resp;
                try
                {
                    resp = await _http.SendAsync(req, cts.Token);
                    json = await resp.Content.ReadAsStringAsync(cts.Token);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("[JOUR status] timeout");
                    if (attempt + 1 < MaxRetries) continue;
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JOUR status] http_error='{ex.Message}'");
                    if (attempt + 1 < MaxRetries) continue;
                    throw;
                }

                if (verbose) Console.WriteLine($"[JOUR status] {(int)resp.StatusCode} len={json.Length}");

                if (!resp.IsSuccessStatusCode)
                {
                    if ((int)resp.StatusCode == 429 && attempt + 1 < MaxRetries)
                    {
                        var delayMs = (int)Math.Pow(2, attempt) * 1000;
                        await Task.Delay(delayMs, ct);
                        continue;
                    }
                    throw new InvalidOperationException($"OpenAI {(int)resp.StatusCode}: {json}");
                }

                var payload = ResponseTextExtractor.Extract(json);
                if (string.IsNullOrWhiteSpace(payload))
                {
                    Console.WriteLine("[JOUR parse] empty payload");
                    if (attempt + 1 < MaxRetries) continue;
                    throw new InvalidOperationException("Empty payload");
                }

                var rewritten = JsonSerializer.Deserialize<News>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                               ?? throw new InvalidOperationException("Invalid JSON");

                rewritten.Source = source.Source;
                rewritten.Url = source.Url;
                rewritten.PublishedAt = source.PublishedAt == default ? DateTime.UtcNow : source.PublishedAt;
                if (rewritten.CreatedAt == default) rewritten.CreatedAt = DateTime.UtcNow;

                Validate(rewritten);
                return rewritten;
            }

            throw new InvalidOperationException("OpenAI retry limit reached.");
        }

        private static void Validate(News n)
        {
            if (string.IsNullOrWhiteSpace(n.Title)) throw new InvalidOperationException("Title required");
            if (string.IsNullOrWhiteSpace(n.Content)) throw new InvalidOperationException("Content required");
            if (string.IsNullOrWhiteSpace(n.Source)) throw new InvalidOperationException("Source required");
            if (string.IsNullOrWhiteSpace(n.Url)) throw new InvalidOperationException("Url required");
        }
    }
}
