using MDS.Application.Abstractions.Integrations;
using MeDeixaSaber.Core.Models;

namespace MDS.Infrastructure.Integrations.NewsApi;

public sealed class NullNewsProvider : INewsProvider
{
    public Task<IReadOnlyList<OutsideNews>> GetTopHeadlinesAsync(int pageSize, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OutsideNews>>(Array.Empty<OutsideNews>());
}
