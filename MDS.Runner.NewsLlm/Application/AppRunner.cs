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

    private static string KeyFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return url.Trim();
        var host = u.Host.Replace("www.", "", StringComparison.OrdinalIgnoreCase);
        var path = u.AbsolutePath.TrimEnd('/');
        return $"{host}{path}";
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var payload = await _collector.RunAsync("endpoint-newsapi-org-everything", ct);
        if (payload is null) return 0;

        var apiCount = payload.Articles?.Count ?? 0;
        Console.WriteLine($"[APP] artigos_api={apiCount}");

        var selectedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (apiCount >= 20 && payload.Articles is not null)
        {
            var idx = new List<int>(apiCount);
            for (var i = 0; i < apiCount; i++) idx.Add(i);
            for (var i = idx.Count - 1; i > 0; i--)
            {
                var j = Random.Shared.Next(i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }
            var take = Math.Min(20, apiCount);
            for (var i = 0; i < take; i++)
            {
                var k = KeyFromUrl(payload.Articles[idx[i]].Url);
                if (k.Length > 0) selectedKeys.Add(k);
            }
        }

        var useFilter = selectedKeys.Count > 0;
        var limit = useFilter ? selectedKeys.Count : 20;

        var ok = 0;
        var fail = 0;

        await foreach (var item in _journalist.StreamWriteAsync(payload, EditorialBias.Neutro, ct))
        {
            ct.ThrowIfCancellationRequested();
            if (ok >= limit) break;

            var allow = !useFilter;
            if (useFilter)
            {
                var key = KeyFromUrl(item.Url);
                allow = key.Length > 0 && selectedKeys.Contains(key);
            }
            if (!allow) continue;

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

        Console.WriteLine($"[APP] persistidos_ok={ok} persistidos_fail={fail}");
        return ok;
    }
}
