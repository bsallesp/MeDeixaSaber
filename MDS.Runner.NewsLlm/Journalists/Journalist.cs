using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public interface IJournalist
    {
        Task<IReadOnlyCollection<News>> WriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default);
    }

    public sealed class Journalist(INewsMapper mapper, IOpenAiNewsRewriter rewriter) : IJournalist
    {
        private readonly INewsMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        private readonly IOpenAiNewsRewriter _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));

        public async Task<IReadOnlyCollection<News>> WriteAsync(NewsApiResponse payload, EditorialBias bias, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(payload);
            var items = (payload.Articles)
                .Take(10) 
                .ToList();

            var results = new List<News>(capacity: items.Count);
            foreach (var src in items.Select(a => _mapper.Map(a)).OfType<News>())
            {
                try
                {
                    var n = await _rewriter.RewriteAsync(src, bias, ct);
                    results.Add(n);

                    Console.WriteLine($"Nova news: {n.Title} /// {n.Content}");
                }
                catch
                {
                    // ignored
                }
            }
            return results;
        }
    }
}