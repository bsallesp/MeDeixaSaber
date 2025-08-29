using MDS.Infrastructure.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Interfaces;

public interface IJournalist
{
    Task<IReadOnlyCollection<OutsideNews>> WriteAsync(NewsApiResponseDto payload, EditorialBias bias, CancellationToken ct = default);
    IAsyncEnumerable<OutsideNews> StreamWriteAsync(NewsApiResponseDto payload, EditorialBias bias, CancellationToken ct = default);
}