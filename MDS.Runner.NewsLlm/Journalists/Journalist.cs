using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public sealed class Journalist(INewsMapper mapper, IOpenAiNewsRewriter rewriter) : IJournalist
    {
        private readonly INewsMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IOpenAiNewsRewriter _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));

        private const int MaxArticlesPerRun = 12;
        private const int MaxConsecutiveTimeouts = 1;
        private static readonly TimeSpan PerItemTimeout = TimeSpan.FromSeconds(8);

        public async Task<IReadOnlyCollection<News>> WriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var articles = payload.Articles ?? [];
            if (articles.Count == 0) return [];

            var sources = articles
                .OrderByDescending(a => a.PublishedAt)
                .Take(MaxArticlesPerRun)
                .Select(a => _mapper.Map(a))
                .OfType<News>()
                .ToList();

            var results = new List<News>(capacity: sources.Count);
            var consecutiveTimeouts = 0;
            var rewriteEnabled = !IsRewriteDisabledByEnv();

            foreach (var src in sources)
            {
                ct.ThrowIfCancellationRequested();

                if (!rewriteEnabled)
                {
                    results.Add(BuildFallback(src));
                    continue;
                }

                Console.WriteLine($"[JOUR try] {src.Title}");
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(PerItemTimeout);
                    var rewritten = await _rewriter.RewriteAsync(src, bias, cts.Token);
                    results.Add(rewritten);
                    consecutiveTimeouts = 0;
                    Console.WriteLine($"[JOUR ok] {src.Title}");
                }
                catch (TaskCanceledException)
                {
                    consecutiveTimeouts++;
                    Console.WriteLine($"[JOUR fail-timeout] {src.Title}");
                    results.Add(BuildFallback(src));

                    if (consecutiveTimeouts >= MaxConsecutiveTimeouts)
                    {
                        Console.WriteLine("[JOUR circuit-open] desativando rewrite para o restante desta execução");
                        rewriteEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JOUR fail] {src.Title} err='{ex.Message}'");
                    results.Add(BuildFallback(src));
                }
            }

            Console.WriteLine($"[JOUR done] produzidos={results.Count}");
            return results;
        }

        private static bool IsRewriteDisabledByEnv()
        {
            var v = Environment.GetEnvironmentVariable("MDS_DISABLE_REWRITE");
            return !string.IsNullOrWhiteSpace(v) && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));
        }

        private static News BuildFallback(News src)
        {
            return new News
            {
                Title = src.Title,
                Summary = string.IsNullOrWhiteSpace(src.Summary)
                    ? (string.IsNullOrWhiteSpace(src.Content) ? null : (src.Content.Length > 220 ? src.Content[..220] : src.Content))
                    : src.Summary,
                Content = string.IsNullOrWhiteSpace(src.Content) ? "(sem conteúdo)" : src.Content,
                Source = string.IsNullOrWhiteSpace(src.Source) ? "(unknown)" : src.Source,
                Url = src.Url,
                PublishedAt = src.PublishedAt == default ? DateTime.UtcNow : src.PublishedAt,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
