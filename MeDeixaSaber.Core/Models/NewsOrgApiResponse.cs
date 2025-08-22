using System.Text.Json.Serialization;

namespace MeDeixaSaber.Core.Models
{
    public sealed class NewsApiResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;

        [JsonPropertyName("totalResults")]
        public int TotalResults { get; init; }

        [JsonPropertyName("articles")]
        public List<NewsArticle> Articles { get; init; } = new();
    }

    public sealed class NewsArticle
    {
        [JsonPropertyName("source")]
        public NewsSource? Source { get; init; }

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

    public sealed class NewsSource
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}