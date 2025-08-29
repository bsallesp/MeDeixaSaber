using System.Text.Json.Serialization;

namespace MDS.Infrastructure.Integrations.NewsApi.Dto;

public sealed class NewsSourceDto
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}