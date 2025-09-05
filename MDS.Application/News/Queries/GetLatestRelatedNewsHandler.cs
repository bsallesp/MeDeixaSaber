using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Models;

namespace MDS.Application.News.Queries;

public sealed class GetLatestRelatedNewsHandler(INewsRelatedReadRepository repo)
    : IQueryHandler<GetLatestRelatedNewsQuery, IReadOnlyList<NewsRow>>
{
    public Task<IReadOnlyList<NewsRow>> Handle(GetLatestRelatedNewsQuery q, CancellationToken ct)
        => repo.GetLatestRelatedAsync(q.DaysBack, q.TopN, q.UseContent, ct);
}