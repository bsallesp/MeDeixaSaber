using MDS.Application.News.Models;

namespace MDS.Application.Abstractions.Data;

public interface INewsRelatedReadRepository
{
    Task<IReadOnlyList<NewsRow>> GetLatestRelatedAsync(
        int daysBack, int topN, bool useContent, CancellationToken ct = default);
}