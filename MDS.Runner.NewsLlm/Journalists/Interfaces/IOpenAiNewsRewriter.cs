using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Interfaces;

public interface IOpenAiNewsRewriter
{
    Task<OutsideNews> RewriteAsync(OutsideNews source, EditorialBias bias, CancellationToken ct = default);
}