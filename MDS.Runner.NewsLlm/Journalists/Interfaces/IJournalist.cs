using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Interfaces;

public interface IJournalist
{
    Task<IReadOnlyCollection<News>> WriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default);
    IAsyncEnumerable<News> StreamWriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default);
}