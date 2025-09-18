using System.Text.Json;
using MDS.Runner.NewsLlm.Integrations.Gemini;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Gemini
{
    public sealed class GeminiJournalistService : IGeminiJournalistService
    {
        private readonly IGeminiService _gemini;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public GeminiJournalistService(IGeminiService gemini)
        {
            _gemini = gemini ?? throw new ArgumentNullException(nameof(gemini));
        }

        public async Task<List<string>> DiscoverTopicsAsync(CancellationToken ct = default)
        {
            const string prompt = @"
                Aja como um editor de um portal de notícias para a comunidade brasileira nos EUA.
                Usando a busca, encontre 5 manchetes de notícias recentes e de alto impacto. 
                É CRÍTICO que você tente cobrir as 5 categorias a seguir, com uma manchete para cada:
                1. Imigração & Leis: Foco em mudanças de vistos, regras de entrada e imigração.
                2. Política & Diplomacia: Foco em relações EUA-Brasil e política americana com grande repercussão.
                3. Economia & Finanças: Foco em taxas de juros, inflação, câmbio (Dólar/Real) e mercado de trabalho local.
                4. Comunidade & Cultura: Foco em eventos culturais, leis estaduais em áreas com alta concentração de brasileiros (Flórida, MA, CA).
                5. Segurança & Direitos: Foco em alertas de golpes, direitos do consumidor e direitos civis.

                Responda APENAS com uma lista de 5 strings em formato JSON, como neste exemplo: [""Manchete 1 (Imigração)"", ""Manchete 2 (Política)"", ""Manchete 3 (Economia)"", ""Manchete 4 (Comunidade)"", ""Manchete 5 (Segurança)""]";

            var response = await _gemini.GenerateContentAsync(prompt, true, ct);
            var text = ExtractTextFromResponse(response);
            if (string.IsNullOrWhiteSpace(text)) return [];
            
            var cleanedText = CleanJsonString(text);

            try
            {
                return JsonSerializer.Deserialize<List<string>>(cleanedText, JsonOptions) ?? [];
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[GEMINI PARSE ERROR] Failed to parse headlines: {ex.Message}. Raw text: {text}");
                return [];
            }
        }

        public async Task<string?> ResearchTopicAsync(string headline, CancellationToken ct = default)
        {
            var prompt = $@"
                Pesquise detalhadamente a notícia: '{headline}'.
                Reúna em formato de texto corrido todos os fatos essenciais, dados, números, pessoas envolvidas e contexto necessário para um jornalista escrever uma matéria completa sobre o assunto.
                Seja factual e direto.";

            var response = await _gemini.GenerateContentAsync(prompt, true, ct);
            return ExtractTextFromResponse(response);
        }

        public async Task<OutsideNews?> WriteArticleAsync(string researchData, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(researchData) || researchData.Length < 200)
            {
                Console.WriteLine("[GEMINI WRITER] Recusando escrita: researchData muito curto ou vazio.");
                return null;
            }

            var prompt = GetWritingPrompt(researchData);
            
            var response = await _gemini.GenerateContentAsync(prompt, false, ct);
            var text = ExtractTextFromResponse(response);
            
            if (string.IsNullOrWhiteSpace(text)) return null;

            var cleanedText = CleanJsonString(text);
            
            OutsideNews? article = null;
            
            try
            {
                article = DeserializeArticle(cleanedText);
            }
            catch (JsonException)
            {
                Console.WriteLine("[GEMINI WRITER] Primeira tentativa falhou (JSON inválido). Tentando correção...");
                
                var correctionPrompt = GetCorrectionPrompt(cleanedText);
                var correctionResponse = await _gemini.GenerateContentAsync(correctionPrompt, false, ct);
                var correctionText = ExtractTextFromResponse(correctionResponse);
                
                if (string.IsNullOrWhiteSpace(correctionText)) return null;

                var finalCleanedText = CleanJsonString(correctionText);
                
                try
                {
                    article = DeserializeArticle(finalCleanedText);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[GEMINI PARSE ERROR] A correção falhou: {ex.Message}. Texto Bruto: {finalCleanedText}");
                    return null;
                }
            }

            if (article != null && !string.IsNullOrWhiteSpace(article.Title) && !string.IsNullOrWhiteSpace(article.Url))
            {
                // Os valores CreatedAt e PublishedAt são definidos dentro de DeserializeArticle/Cleanup
                return article;
            }
            return null;
        }

        private OutsideNews? DeserializeArticle(string cleanedText)
        {
            var article = JsonSerializer.Deserialize<OutsideNews>(cleanedText, JsonOptions);
            
            if (article != null)
            {
                if (article.CreatedAt == null) article.CreatedAt = DateTime.UtcNow;
                if (article.PublishedAt == null) article.PublishedAt = DateTime.UtcNow;

                using var doc = JsonDocument.Parse(cleanedText);
                var categoryName = doc.RootElement.TryGetProperty("CategoryName", out var catEl) 
                                   && catEl.ValueKind == JsonValueKind.String ? catEl.GetString() : null;

                article.Categories.Clear(); 

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var tempCategory = new Category { Name = categoryName, Slug = categoryName.ToLowerInvariant().Replace(' ', '-') };
        
                    article.Categories.Add(tempCategory);
                }
            }
            return article;
        }

        private static string GetWritingPrompt(string researchData) => $@"
            Aja como um jornalista sênior escrevendo para um portal de notícias voltado a brasileiros que moram nos EUA. O tom deve ser útil e direto.
            Com base estritamente nas informações pesquisadas abaixo, escreva uma matéría jornalística completa.
            O campo 'Content' deve ter entre 400 e 600 palavras.
            **REGRA CRÍTICA: Se as informações pesquisadas não forem suficientes para escrever um artigo completo, NÃO crie um artigo sobre a falta de dados. Simplesmente retorne um JSON com todos os campos nulos ou vazios. O campo 'ImageUrl' DEVE ser preenchido com null.**
            **CATEGORIZAÇÃO: Com base no título, identifique a categoria principal (apenas UMA). O campo 'CategoryName' deve conter o nome exato de uma das categorias: 'Imigração & Leis', 'Política & Diplomacia', 'Economia & Finanças', 'Comunidade & Cultura', ou 'Segurança & Direitos'.**

            INFORMAÇÕES PESQUISADAS:
            ---
            {researchData}
            ---

            Responda APENAS com um objeto JSON estrito, sem markdown, comentários ou texto extra, com a seguinte estrutura:
            {{
              ""Title"": ""string"",
              ""Summary"": ""string|null"",
              ""Content"": ""string"",
              ""Source"": ""string (nome do principal veículo de notícias consultado)"",
              ""Url"": ""string (URL da principal fonte consultado)"",
              ""ImageUrl"": ""string|null"",
              ""PublishedAt"": ""ISO-8601 UTC datetime"",
              ""CreatedAt"": ""ISO-8601 UTC datetime"",
              ""CategoryName"": ""string""
            }}";

        private static string GetCorrectionPrompt(string invalidJson) => $@"
            O JSON fornecido está inválido e não pode ser desserializado. Sua ÚNICA tarefa é corrigir a sintaxe do JSON e garantir que ele seja estrito, sem markdown, comentários ou texto extra.

            JSON Inválido:
            ---
            {invalidJson}
            ---

            Responda APENAS com o objeto JSON estrito e válido.
        ";

        private static string? ExtractTextFromResponse(Integrations.Gemini.Dto.GeminiResponseDto? response)
        {
            return response?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        }

        private static string CleanJsonString(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return rawText;

            var trimmed = rawText.Trim();
            
            var firstBracket = trimmed.IndexOf('[');
            var firstBrace = trimmed.IndexOf('{');
            
            int startIndex;
            if (firstBrace != -1) startIndex = firstBrace;
            else if (firstBracket != -1) startIndex = firstBracket;
            else startIndex = -1;

            if (startIndex == -1) return trimmed;

            var lastBracket = trimmed.LastIndexOf(']');
            var lastBrace = trimmed.LastIndexOf('}');
            
            var endIndex = Math.Max(lastBracket, lastBrace);
            
            if (endIndex == -1 || endIndex < startIndex) return trimmed;

            return trimmed.Substring(startIndex, endIndex - startIndex + 1);
        }
    }
}