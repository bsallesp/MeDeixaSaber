using MDS.Runner.NewsLlm.Abstractions;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application;

public sealed class CompositeArticleSink(IReadOnlyList<IArticleSink> sinks) : IArticleSink
{
    readonly IReadOnlyList<IArticleSink> _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
    public async Task InsertAsync(OutsideNews item)
    {
        foreach (var s in _sinks) await s.InsertAsync(item);
    }
}