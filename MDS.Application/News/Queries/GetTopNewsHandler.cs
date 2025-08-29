using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MeDeixaSaber.Core.Models;

namespace MDS.Application.News.Queries;

public sealed record GetTopNewsQuery(int PageSize) : IQuery<IReadOnlyList<OutsideNews>>;

public sealed class GetTopNewsHandler(IOutsideNewsReadRepository repo)
    : IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>
{
    public Task<IReadOnlyList<OutsideNews>> Handle(GetTopNewsQuery request, CancellationToken cancellationToken)
        => repo.GetTopAsync(request.PageSize, cancellationToken);
}
