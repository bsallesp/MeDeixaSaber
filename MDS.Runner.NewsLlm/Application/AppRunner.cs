using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application;

public sealed class AppRunner(
    INewsOrgCollector collector,
    IOpenAiNewsRewriter rewriter,
    INewsMapper mapper,
    IJournalist journalist,
    IArticleSink sink,
    IArticleRead reader) : IAppRunner
{
    private readonly INewsOrgCollector _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    private readonly IOpenAiNewsRewriter _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));
    private readonly INewsMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IArticleSink _sink = sink ?? throw new ArgumentNullException(nameof(sink));
    private readonly IArticleRead _reader = reader ?? throw new ArgumentNullException(nameof(reader));

    private const int DesiredNewArticles = 10;

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var payload = await _collector.RunAsync("endpoint-newsapi-org-everything", ct);
        if (payload is null) return 0;

        var created = 0;

        foreach (var raw in payload.Articles ?? [])
        {
            if (ct.IsCancellationRequested) break;
            if (created >= DesiredNewArticles) break;
            if (string.IsNullOrWhiteSpace(raw.Url)) continue;

            var exists = await _reader.ExistsByUrlAsync(raw.Url);
            if (exists) continue;

            var mapped = _mapper.Map(raw);
            var rewritten = await _rewriter.RewriteAsync(mapped, EditorialBias.Neutro, ct);
            if (rewritten is null) continue;

            try
            {
                await _sink.InsertAsync(rewritten);
                created++;
            }
            catch { }
        }

        return created;
    }
}