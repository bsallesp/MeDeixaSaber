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
            instruction = "Reescreva a notícia mantendo fatos, acrescentando contexto quando claro, sem inventar. Copie a imagem quando houver.",
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
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "outside_news",
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        required = new[] { "Title", "Content", "Source", "Url", "PublishedAt", "CreatedAt" },
                        properties = new
                        {
                            Title = new { type = "string" },
                            Summary = new { type = new[] { "string", "null" } },
                            Content = new { type = "string" },
                            Source = new { type = "string" },
                            Url = new { type = "string" },
                            ImageUrl = new { type = new[] { "string", "null" } },
                            PublishedAt = new { type = "string", format = "date-time" },
                            CreatedAt = new { type = "string", format = "date-time" }
                        }
                    },
                    strict = true
                }
            },
            messages = new object[]
            {
                new { role = "system", content = sys },
                new { role = "user", content = JsonSerializer.Serialize(userObj) }
            }
        };

        using var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses")
        {
            Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json")
        };
        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        var httpResp = await _http.SendAsync(httpReq, ct);
        if (!httpResp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Rewrite failed: {(int)httpResp.StatusCode}");

        var json = await httpResp.Content.ReadAsStringAsync(ct);
        if (verbose) Console.WriteLine($"[JOUR status] {(int)httpResp.StatusCode} len={json.Length}");

        var text = ResponseTextExtractor.Extract(json);
        if (verbose) Console.WriteLine($"[JOUR parse] payload_len={(text?.Length ?? 0)}");

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Rewrite returned empty output");

        text = text.Replace("“", "\"").Replace("”", "\"").Replace("’", "'");
        text = text.Trim();
        var i = text.IndexOf('{');
        var j = text.LastIndexOf('}');
        if (i >= 0 && j > i) text = text.Substring(i, j - i + 1);

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
            throw new InvalidOperationException("Rewrite returned null JSON");

        if (string.IsNullOrWhiteSpace(news.Title)) news.Title = original.Title;
        if (string.IsNullOrWhiteSpace(news.Source)) news.Source = string.IsNullOrWhiteSpace(original.Source) ? "(unknown)" : original.Source;
        if (string.IsNullOrWhiteSpace(news.Url)) news.Url = original.Url;
        if (string.IsNullOrWhiteSpace(news.ImageUrl)) news.ImageUrl = original.ImageUrl;
        if (news.PublishedAt == default) news.PublishedAt = original.PublishedAt == default ? DateTime.UtcNow : original.PublishedAt;
        if (news.CreatedAt == default) news.CreatedAt = DateTime.UtcNow;

        return news;
    }
}
