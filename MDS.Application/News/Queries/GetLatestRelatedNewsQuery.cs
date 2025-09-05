using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Models;

namespace MDS.Application.News.Queries;

public sealed record GetLatestRelatedNewsQuery(int DaysBack, int TopN, bool UseContent)
    : IQuery<IReadOnlyList<NewsRow>>;