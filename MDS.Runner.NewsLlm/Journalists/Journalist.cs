using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public sealed class Journalist(INewsMapper mapper, IOpenAiNewsRewriter rewriter) : IJournalist
    {
        private readonly INewsMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IOpenAiNewsRewriter _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));

        private const int MaxArticlesPerRun = 12;
        private const int MaxConsecutiveFailures = 3;

        public async Task<IReadOnlyCollection<News>> WriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default)
        {
            var list = new List<News>();
            await foreach (var n in StreamWriteAsync(payload, bias, ct)) list.Add(n);
            return list;
        }

        public async IAsyncEnumerable<News> StreamWriteAsync(NewsApiResponse payload, EditorialBias bias, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            if (IsRewriteDisabledByEnv()) yield break;

            var articles = payload.Articles ?? [];
            if (articles.Count == 0) yield break;

            var sources = articles
                .OrderByDescending(a => a.PublishedAt)
                .Take(MaxArticlesPerRun)
                .Select(a => _mapper.Map(a))
                .OfType<News>()
                .ToList();

            var consecutiveFailures = 0;

            foreach (var src in sources)
            {
                ct.ThrowIfCancellationRequested();

                News? rewritten = null;
                bool shouldYield = false;

                try
                {
                    rewritten = await _rewriter.RewriteAsync(src, bias, ct);
                    consecutiveFailures = 0;
                    Console.WriteLine($"[JOUR ok] {src.Title}");
                    shouldYield = true;
                }
                catch (TaskCanceledException)
                {
                    consecutiveFailures++;
                    Console.WriteLine($"[JOUR timeout] {src.Title}");
                    if (consecutiveFailures >= MaxConsecutiveFailures) yield break;
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    Console.WriteLine($"[JOUR fail] {src.Title} err='{ex.Message}'");
                    if (consecutiveFailures >= MaxConsecutiveFailures) yield break;
                }

                if (shouldYield && rewritten is not null)
                    yield return rewritten;
            }

            Console.WriteLine("[JOUR done-stream]");
        }


        private static bool IsRewriteDisabledByEnv()
        {
            var v = Environment.GetEnvironmentVariable("MDS_DISABLE_REWRITE");
            return !string.IsNullOrWhiteSpace(v) && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
    }
}
