using System.Text.Json.Serialization;

namespace MDS.Infrastructure.Integrations.NewsApi.Dto;

public sealed class NewsApiResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("totalResults")]
    public int TotalResults { get; init; }

    [JsonPropertyName("articles")]
    public List<NewsArticleDto> Articles { get; init; } = new();
}