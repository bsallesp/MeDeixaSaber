using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;

namespace MDS.Api.Tests.Support;

public sealed class ThrowingGetTopNewsHandler : IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>
{
    public Task<IReadOnlyList<OutsideNews>> Handle(GetTopNewsQuery query, CancellationToken ct = default)
    {
        throw new InvalidOperationException("boom");
    }
}