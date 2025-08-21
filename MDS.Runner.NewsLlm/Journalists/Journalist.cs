using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public interface IJournalist
    {
        Task RunAsync(NewsApiResponse payload, CancellationToken ct = default);
    }

    public sealed class Journalist : IJournalist
    {
        public Task RunAsync(NewsApiResponse payload, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(payload);

            var count = payload.Articles?.Count ?? 0;
            Console.WriteLine($"[JOURNALIST start] status={payload.Status} totalResults={payload.TotalResults} articles={count}");

            var top = payload.Articles?.ToList() ?? [];

            Console.WriteLine($"Total articles: {top.Count}");
            
            for (var i = 0; i < top.Count; i++)
            {
                var a = top[i];
                Console.WriteLine($"[JOURNALIST article] #{i + 1} title=\"{a.Title}\" source=\"{a.Source?.Name}\" publishedAt={a.PublishedAt:O}");
            }

            Console.WriteLine("[JOURNALIST done]");
            return Task.CompletedTask;
        }
    }
}