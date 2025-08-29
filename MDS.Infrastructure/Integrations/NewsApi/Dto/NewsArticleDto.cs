using System.Text.Json.Serialization;

namespace MDS.Infrastructure.Integrations.NewsApi.Dto;

public sealed class NewsArticleDto
{
    [JsonPropertyName("source")]
    public NewsSourceDto? Source { get; init; }

    [JsonPropertyName("author")]
    public string? Author { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("urlToImage")]
    public string? UrlToImage { get; init; }

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }
}