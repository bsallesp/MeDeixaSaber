using System.Text.Json.Serialization;

namespace MDS.Runner.NewsLlm.Integrations.Gemini.Dto
{
    public class GeminiResponseDto
    {
        [JsonPropertyName("candidates")]
        public List<CandidateDto> Candidates { get; set; } = [];
    }

    public class CandidateDto
    {
        [JsonPropertyName("content")]
        public ContentDto Content { get; set; } = new();
    }
}