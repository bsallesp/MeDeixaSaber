using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Gemini
{
    public interface IGeminiJournalistService
    {
        Task<List<string>> DiscoverTopicsAsync(CancellationToken ct = default);
        Task<string?> ResearchTopicAsync(string headline, CancellationToken ct = default);
        Task<OutsideNews?> WriteArticleAsync(string researchData, CancellationToken ct = default);
    }
}