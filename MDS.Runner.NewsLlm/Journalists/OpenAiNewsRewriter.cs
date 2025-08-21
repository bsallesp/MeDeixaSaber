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

        private readonly string _apiKey = string.IsNullOrWhiteSpace(apiKey)
            ? throw new ArgumentException("Required", nameof(apiKey))
            : apiKey;

        private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;

        private const int MaxContentChars = 1500;
        private const int MaxOutputTokens = 900;
        private const int MaxRetries = 3;

        public async Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(source);

            var biasInstr = bias switch
            {
                EditorialBias.Sensacionalista => "linguagem dramática e chamativa, títulos de forte impacto, ênfase em conflito ou emoção (sem inventar fatos)",
                EditorialBias.Conservador     => "foco em lei, ordem, tradição e responsabilidade institucional; tom sério e cauteloso",
                EditorialBias.Progressista    => "foco em direitos civis, justiça social e impacto humano; tom empático e inclusivo",
                EditorialBias.Agressivo       => "frases curtas, diretas, tom combativo e assertivo",
                _                             => "tom neutro, objetivo e responsável, com clareza jornalística"
            };

            var desc = source.Summary ?? string.Empty;
            var content = source.Content ?? string.Empty;
            if (content.Length > MaxContentChars) content = content[..MaxContentChars];

            var prompt = $@"Reescreva a notícia abaixo em português (pt-BR), {biasInstr}.
                Produza 450–650 palavras, sem inventar fatos. Escreva em formato jornalístico PROFISSIONAL:
                - Título informativo e conciso (sem sensacionalismo).
                - Lide objetivo no 1º parágrafo (quem, o quê, quando, onde e por quê).
                - Corpo em 2–4 parágrafos: contexto, números, datas, nomes e medidas concretas.
                - Se houver na matéria original, inclua 1–2 citações ou paráfrases de autoridades/especialistas.
                - Evite opinião e adjetivação moralizante; não use perguntas retóricas; não repita ideias.

                Mantenha estes campos exatamente:
                - Source: ""{source.Source}""
                - Url: ""{source.Url}""
                - PublishedAt: ""{source.PublishedAt:yyyy-MM-ddTHH:mm:ssZ}""

                Regras:
                - Use SOMENTE informações presentes em 'Dados' e no 'Trecho do conteúdo' abaixo.
                - Não crie fatos, pesquisas, números ou aspas novas.
                - Respeite a ortografia pt-BR; parágrafos curtos (3–5 linhas).
                - Resumo (Summary) claro em 1–2 frases.

                Dados:
                Título original: {source.Title}
                Resumo original: {desc}

                Trecho do conteúdo (base factual, sem traduções literais):
                {content}

                Responda APENAS com JSON válido no formato:
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
                // ✅ inclua Summary no required
                required = new[] { "Title", "Summary", "Content", "Source", "Url", "PublishedAt", "CreatedAt" },
                properties = new
                {
                    Title = new { type = "string" },
                    Summary = new { type = "string" },
                    Content = new { type = "string" },
                    Source = new { type = "string", const_ = source.Source },
                    Url = new { type = "string", const_ = source.Url },
                    PublishedAt = new
                    {
                        type = "string",
                        const_ = source.PublishedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
                    },
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
                req.Content = new StringContent(
                    JsonSerializer.Serialize(body).Replace("\"const_\"", "\"const\""),
                    Encoding.UTF8,
                    "application/json");

                if (verbose) Console.WriteLine($"[JOUR request] model={_model} bias={bias} title={source.Title}");

                using var resp = await _http.SendAsync(req, ct);

                var json = await resp.Content.ReadAsStringAsync(ct);
                if (verbose) Console.WriteLine($"[JOUR status] {(int)resp.StatusCode} len={json.Length}");
                if (verbose && !resp.IsSuccessStatusCode)
                    Console.WriteLine($"[JOUR error] status={(int)resp.StatusCode} body={json}");

                if ((int)resp.StatusCode == 429 && attempt + 1 < MaxRetries)
                {
                    var delayMs = (int)Math.Pow(2, attempt) * 1000; // 1s, 2s
                    await Task.Delay(delayMs, ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                    throw new InvalidOperationException($"OpenAI {(int)resp.StatusCode}: {json}");

                var payload = ResponseTextExtractor.Extract(json);
                if (string.IsNullOrWhiteSpace(payload))
                    throw new InvalidOperationException("Empty payload");

                var rewritten = JsonSerializer.Deserialize<News>(payload,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
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