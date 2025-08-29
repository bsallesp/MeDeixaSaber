using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MeDeixaSaber.Core.Models;

namespace MDS.Application.News.Queries;

public sealed record GetTopNewsQuery(int PageSize) : IQuery<IReadOnlyList<OutsideNews>>;

public sealed class GetTopNewsHandler : IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>
{
    private readonly IOutsideNewsReadRepository _repo;

    public GetTopNewsHandler(IOutsideNewsReadRepository repo)
    {
        _repo = repo;
    }

    public Task<IReadOnlyList<OutsideNews>> Handle(GetTopNewsQuery request, CancellationToken cancellationToken)
        => _repo.GetTopAsync(request.PageSize, cancellationToken);
}
