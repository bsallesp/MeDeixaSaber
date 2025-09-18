using System.Text.Json;
using MDS.Runner.NewsLlm.Integrations.Gemini;
using MeDeixaSaber.Core.Models;
using System.Text;

namespace MDS.Runner.NewsLlm.Journalists.Gemini
{
    public sealed class GeminiJournalistService : IGeminiJournalistService
    {
        private readonly IGeminiService _gemini;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        
        private static readonly IReadOnlyList<string> ResearchSources = 
        [
            "Globo",
            "UOL",
            "Estadao",
            "Folha de S.Paulo",
            "The New York Times",
            "The Guardian",
            "Reuters",
            "BBC News Brasil"
        ];
        private readonly Random _random = new();

        public GeminiJournalistService(IGeminiService gemini)
        {
            _gemini = gemini ?? throw new ArgumentNullException(nameof(gemini));
        }

        public async Task<List<string>> DiscoverTopicsAsync(CancellationToken ct = default)
        {
            const string prompt = @"
                Aja como um editor de um portal de notícias para a comunidade brasileira nos EUA.
                Usando a busca, encontre 5 manchetes de notícias recentes e de alto impacto que sejam **DIRETAMENTE RELEVANTES** para brasileiros vivendo no exterior. 
                
                Priorize temas como: IMIGRAÇÃO, leis de visto, economia local (EUA/Outros países), cultura brasileira na diáspora, e alertas de segurança/golpes financeiros internacionais. **NÃO** inclua política ou geopolítica brasileira/mundial geral.

                É CRUCIAL que você retorne APENAS uma ARRAY JSON, sem comentários, sem markdown, sem introdução ou conclusão.
                
                Exemplo do formato esperado: [ ""Manchete 1"", ""Manchete 2"", ... ]

                Lembre-se: Use SOMENTE a ferramenta de busca ('google_search_retrieval') para responder.";

            var response = await _gemini.GenerateContentAsync(prompt, useSearch: true, ct);
            if (response?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text is not { } jsonRaw)
            {
                Console.WriteLine("[DISCOVERY ERROR] Failed to get response from Gemini.");
                return [];
            }

            var payload = TrySliceFirstJsonArray(jsonRaw) ?? jsonRaw;
            
            try
            {
                var topics = JsonSerializer.Deserialize<List<string>>(payload, JsonOptions);
                Console.WriteLine($"[DISCOVERY OK] Extracted {topics?.Count ?? 0} topics.");
                return topics ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY JSON ERROR] Failed to deserialize topics: {ex.Message}. Raw: {payload.Substring(0, Math.Min(payload.Length, 200))}");
                return [];
            }
        }

        public async Task<string?> ResearchTopicAsync(string topic, CancellationToken ct = default)
        {
            var randomSource = ResearchSources[_random.Next(ResearchSources.Count)];

            var prompt = $@"
                Faça uma pesquisa aprofundada sobre o tópico: ""{topic}"".
                Use SOMENTE a ferramenta de busca ('google_search_retrieval').
                Inclua termos de pesquisa que priorizem a fonte ""{randomSource}"".
                Consolide a informação em um texto único, fluente e informativo em português (mínimo de 300 palavras). 
                Não use introduções nem formatação (markdown, listas, cabeçalhos). Retorne APENAS o texto puro.";

            var response = await _gemini.GenerateContentAsync(prompt, useSearch: true, ct);
            var text = response?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text;
            
            Console.WriteLine($"[RESEARCH OK] Research data length: {text?.Length ?? 0}");
            return text;
        }

        public async Task<OutsideNews?> WriteArticleAsync(string researchData, CancellationToken ct = default)
        {
            var prompt = $@"
                Com base no texto de pesquisa a seguir, escreva um artigo de notícias completo em português. 
                O tom deve ser jornalístico, objetivo e neutro.
                A categoria principal deve ser a mais relevante (ex: Imigracao, Economia, Cultura).
                O campo 'Content' deve ter entre 600 e 900 palavras.

                Retorne APENAS um objeto JSON STICTO (sem markdown, sem comentários, sem aspas triplas), seguindo a estrutura:

                {{
                  ""Title"": ""<título atraente>"",
                  ""Summary"": ""<resumo conciso, 1-2 frases>"",
                  ""Content"": ""<conteúdo completo>"",
                  ""Source"": ""Jornalista IA - Gemini"",
                  ""Url"": ""urn:news:gemini:{Guid.NewGuid():N}"",
                  ""PublishedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
                  ""CreatedAt"": ""{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"",
                  ""Categories"": [ {{ ""Name"": ""<Categoria>"",""Id"":1 }} ]
                }}
                
                TEXTO DE PESQUISA:
                ---
                {researchData}
                ---
                ";
            
            var response = await _gemini.GenerateContentAsync(prompt, useSearch: false, ct);
            if (response?.Candidates.FirstOrDefault()?.Content.Parts.FirstOrDefault()?.Text is not { } jsonRaw)
            {
                Console.WriteLine("[WRITING ERROR] Failed to get response from Gemini.");
                return null;
            }

            var payload = ExtractTextPayload(jsonRaw) ?? TrySliceFirstJsonObject(jsonRaw);

            if (string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine("[WRITING ERROR] Extracted payload is empty or null.");
                return null;
            }

            var news = DeserializeToNews(payload);
            
            if (news is null || string.IsNullOrWhiteSpace(news.Title) || string.IsNullOrWhiteSpace(news.Content))
            {
                Console.WriteLine("[WRITING ERROR] Deserialization/Validation failed.");
                Console.WriteLine($"[WRITING DEBUG] Payload preview: {payload.Substring(0, Math.Min(payload.Length, 200))}");
                return null;
            }
            
            if (news.PublishedAt == default) news.PublishedAt = DateTime.UtcNow;
            if (news.CreatedAt == default) news.CreatedAt = DateTime.UtcNow;

            return news;
        }

        private static string? ExtractTextPayload(string responseJson)
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
            catch { }

            return null;
        }

        private static string? TrySliceFirstJsonObject(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            return (start >= 0 && end > start) ? s.Substring(start, end - start + 1) : null;
        }

        private static string? TrySliceFirstJsonArray(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Replace("```json", "", StringComparison.OrdinalIgnoreCase);
            s = s.Replace("```", "");
            
            var start = s.IndexOf('[');
            var end = s.LastIndexOf(']');
            return (start >= 0 && end > start) ? s.Substring(start, end - start + 1) : null;
        }

        private static OutsideNews? DeserializeToNews(string payload)
        {
            if (TryDeserialize<OutsideNews>(payload, out var one) && one is not null) return one;
            if (TryDeserialize<JsonElement>(payload, out var el) && el.ValueKind == JsonValueKind.Object)
                return MapLooseObject(el);
            
            return null;
        }

        private static bool TryDeserialize<T>(string json, out T? value)
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

        private static OutsideNews? MapLooseObject(JsonElement obj)
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

            return new OutsideNews
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