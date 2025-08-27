using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application;

public sealed class AppRunner(
    INewsOrgCollector collector,
    IOpenAiNewsRewriter rewriter,
    INewsMapper mapper,
    IJournalist journalist,
    IArticleSink sink)
    : IAppRunner
{
    private readonly INewsOrgCollector _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    private readonly IOpenAiNewsRewriter _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));
    private readonly INewsMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IJournalist _journalist = journalist ?? throw new ArgumentNullException(nameof(journalist));
    private readonly IArticleSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var payload = await _collector.RunAsync("endpoint-newsapi-org-everything", ct);
        if (payload is null) return 0;

        var apiCount = payload.Articles?.Count ?? 0;
        Console.WriteLine($"[APP] artigos_api={apiCount}");

        var rewritten = await _journalist.WriteAsync(payload, EditorialBias.Neutro, ct);
        var rewCount = rewritten?.Count ?? 0;
        Console.WriteLine($"[APP] reescritos={rewCount}");

        var ok = 0;
        var fail = 0;

        if (rewritten != null)
        {
            foreach (var item in rewritten.Take(30))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    Console.WriteLine($"[APP] persistindo: {item.Title}");
                    await _sink.InsertAsync(item);
                    Console.WriteLine($"[APP] persistiu_ok: {item.Url}");
                    ok++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[APP] persistiu_erro: {ex.GetType().Name} msg={ex.Message}");
                    fail++;
                }
            }
        }

        Console.WriteLine($"[APP] persistidos_ok={ok} persistidos_fail={fail}");
        return ok;
    }
}
