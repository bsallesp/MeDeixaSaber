using System.Text.Json.Serialization;

namespace MDS.Runner.NewsLlm.Integrations.Gemini.Dto
{
    public class GeminiRequestDto
    {
        [JsonPropertyName("contents")]
        public List<ContentDto> Contents { get; set; } = [];

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolDto>? Tools { get; set; }
    }

    public class ContentDto
    {
        [JsonPropertyName("parts")]
        public List<PartDto> Parts { get; set; } = [];
    }

    public class PartDto
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    public class ToolDto
    {
        [JsonPropertyName("google_search_retrieval")]
        public object GoogleSearchRetrieval { get; set; } = new();
    }
}