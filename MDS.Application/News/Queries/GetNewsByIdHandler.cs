using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MeDeixaSaber.Core.Models;

namespace MDS.Application.News.Queries;

public sealed class GetNewsByIdHandler(IOutsideNewsReadRepository repository)
    : IQueryHandler<GetNewsByIdQuery, OutsideNews?>
{
    public Task<OutsideNews?> Handle(GetNewsByIdQuery request, CancellationToken cancellationToken)
    {
        return repository.GetByIdAsync(request.Id, cancellationToken);
    }
}