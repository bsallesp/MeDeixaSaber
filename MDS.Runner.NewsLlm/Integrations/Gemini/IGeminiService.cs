using MDS.Runner.NewsLlm.Integrations.Gemini.Dto;

namespace MDS.Runner.NewsLlm.Integrations.Gemini
{
    public interface IGeminiService
    {
        Task<GeminiResponseDto?> GenerateContentAsync(string prompt, bool useSearch, CancellationToken ct = default);
    }
}