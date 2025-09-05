using MDS.Application.Abstractions.Data;
using MDS.Application.News.Models;

namespace MDS.Data.Repositories;

public sealed class NullNewsRelatedReadRepository : INewsRelatedReadRepository
{
    public Task<IReadOnlyList<NewsRow>> GetLatestRelatedAsync(int daysBack, int topN, bool useContent, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NewsRow>>([]);
}