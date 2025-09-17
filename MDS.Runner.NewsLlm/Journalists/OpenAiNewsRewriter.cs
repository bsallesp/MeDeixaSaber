using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists;

public sealed class OpenAiNewsRewriter(
    HttpClient http,
    string openAiKey,
    string model = "gpt-4o-mini",
    bool verbose = false)
    : IOpenAiNewsRewriter
{
    private readonly HttpClient _http = http ?? throw new ArgumentNullException(nameof(http));
    private readonly string _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;

    public async Task<OutsideNews> RewriteAsync(OutsideNews original, EditorialBias bias, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(original);

        var sys = "Você é um redator jornalístico. Reescreva com linguagem clara e neutra. Responda apenas com um JSON único e válido, em uma linha, sem markdown.";
        var biasStr = bias.ToString();

        var userObj = new
        {
            instruction = "Reescreva a notícia mantendo fatos, acrescentando contexto quando claro, sem inventar. Copie a imagem quando houver. O campo 'Content' deve ser detalhado, com um tamanho entre 400 e 600 palavras, emulando um estilo jornalístico completo.",
            original = new
            {
                original.Title,
                original.Summary,
                original.Content,
                original.Source,
                original.Url,
                original.ImageUrl,
                PublishedAt = original.PublishedAt.ToUniversalTime().ToString("o"),
                CreatedAt = original.CreatedAt.ToUniversalTime().ToString("o")
            },
            bias = biasStr,
            output_contract = new
            {
                Title = "string",
                Summary = "string|null",
                Content = "string",
                Source = "string",
                Url = "string",
                ImageUrl = "string|null",
                PublishedAt = "ISO-8601 UTC datetime",
                CreatedAt = "ISO-8601 UTC datetime"
            }
        };

        var req = new
        {
            model = _model,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = sys },
                new { role = "user", content = JsonSerializer.Serialize(userObj) }
            }
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        var httpResp = await _http.SendAsync(httpReq, ct);
        var respBody = await httpResp.Content.ReadAsStringAsync(ct);
        if (!httpResp.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Rewrite failed: {(int)httpResp.StatusCode} body={respBody}");
        }

        if (verbose)
        {
            Console.WriteLine($"[JOUR status] {(int)httpResp.StatusCode} len={respBody.Length}");
        }

        var text = ExtractContent(respBody);
        if (verbose)
        {
            Console.WriteLine($"[JOUR parse] payload_len={(text?.Length ?? 0)}");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Rewrite returned empty output");
        }

        text = text.Replace("“", "\"").Replace("”", "\"").Replace("’", "'");
        text = text.Trim();
        var i = text.IndexOf('{');
        var j = text.LastIndexOf('}');
        if (i >= 0 && j > i)
        {
            text = text.Substring(i, j - i + 1);
        }

        OutsideNews? news;
        try
        {
            news = JsonSerializer.Deserialize<OutsideNews>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Rewrite returned invalid JSON");
        }

        if (news is null)
        {
            throw new InvalidOperationException("Rewrite returned null JSON");
        }

        const int minWords = 300;
        var wordCount = news.Content?.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;

        if (wordCount < minWords)
        {
            throw new InvalidOperationException($"Rewrite failed: content is too short ({wordCount} words).");
        }

        if (string.IsNullOrWhiteSpace(news.Title))
        {
            news.Title = original.Title;
        }
        if (string.IsNullOrWhiteSpace(news.Source))
        {
            news.Source = string.IsNullOrWhiteSpace(original.Source) ? "(unknown)" : original.Source;
        }
        if (string.IsNullOrWhiteSpace(news.Url))
        {
            news.Url = original.Url;
        }
        if (string.IsNullOrWhiteSpace(news.ImageUrl))
        {
            news.ImageUrl = original.ImageUrl;
        }
        if (news.PublishedAt == default)
        {
            news.PublishedAt = original.PublishedAt == default ? DateTime.UtcNow : original.PublishedAt;
        }
        if (news.CreatedAt == default)
        {
            news.CreatedAt = DateTime.UtcNow;
        }

        return news;
    }

    private static string? ExtractContent(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.ValueKind == JsonValueKind.Array && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message) && message.TryGetProperty("content", out var contentEl) && contentEl.ValueKind == JsonValueKind.String)
            {
                return contentEl.GetString();
            }
            if (choice.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
            {
                return textEl.GetString();
            }
        }

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
        {
            var first = output[0];
            if (first.TryGetProperty("content", out var contentArr) && contentArr.ValueKind == JsonValueKind.Array && contentArr.GetArrayLength() > 0)
            {
                var c0 = contentArr[0];
                if (c0.TryGetProperty("text", out var t))
                {
                    if (t.ValueKind == JsonValueKind.String) return t.GetString();
                    if (t.ValueKind == JsonValueKind.Object && t.TryGetProperty("value", out var tv) && tv.ValueKind == JsonValueKind.String) return tv.GetString();
                }
            }
        }

        return null;
    }
}