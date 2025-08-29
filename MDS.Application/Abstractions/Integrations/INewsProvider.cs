using MeDeixaSaber.Core.Models;

namespace MDS.Application.Abstractions.Integrations;

public interface INewsProvider
{
    Task<IReadOnlyList<OutsideNews>> GetTopHeadlinesAsync(int pageSize, CancellationToken ct = default);
}