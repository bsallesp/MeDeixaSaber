using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Journalists.Gemini;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application
{
    public sealed class AppRunner : IAppRunner
    {
        private readonly IGeminiJournalistService _journalist;
        private readonly IArticleSink _sink;
        private readonly IArticleRead _reader;
        private readonly IImageFinder _imageFinder;
        private const int DesiredNewArticles = 5;
        // Definir um limite mínimo de caracteres para a pesquisa (ex: 200) para evitar artigos baseados em 1 frase.
        private const int MinResearchDataLength = 200; 

        public AppRunner(
            IGeminiJournalistService journalist,
            IArticleSink sink,
            IArticleRead reader,
            IImageFinder imageFinder)
        {
            _journalist = journalist ?? throw new ArgumentNullException(nameof(journalist));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _imageFinder = imageFinder ?? throw new ArgumentNullException(nameof(imageFinder));
        }

        public async Task<int> RunAsync(CancellationToken ct = default)
        {
            Console.WriteLine("[APP RUNNER] Starting 3-phase Gemini workflow...");

            var headlines = await _journalist.DiscoverTopicsAsync(ct);
            if (headlines is not { Count: > 0 })
            {
                Console.WriteLine("[APP RUNNER] Phase 1 (Discovery) did not return any topics.");
                return 0;
            }

            Console.WriteLine($"[APP RUNNER] Phase 1 discovered {headlines.Count} potential topics.");

            var createdCount = 0;
            foreach (var headline in headlines)
            {
                if (ct.IsCancellationRequested || createdCount >= DesiredNewArticles)
                {
                    break;
                }

                Console.WriteLine($"[APP RUNNER] Starting Phase 2 (Research) for: \"{headline}\"");
                var researchData = await _journalist.ResearchTopicAsync(headline, ct);
                
                if (string.IsNullOrWhiteSpace(researchData) || researchData.Length < MinResearchDataLength)
                {
                    Console.WriteLine($"[APP RUNNER] Phase 2 did not produce enough research data ({researchData?.Length ?? 0} chars) for \"{headline}\". Skipping.");
                    continue;
                }

                Console.WriteLine($"[APP RUNNER] Starting Phase 3 (Writing) for: \"{headline}\"");
                var finalArticle = await _journalist.WriteArticleAsync(researchData, ct);
                if (finalArticle is null)
                {
                    Console.WriteLine($"[APP RUNNER] Phase 3 did not produce a valid article for \"{headline}\". Skipping.");
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(finalArticle.ImageUrl))
                {
                    Console.WriteLine($"[APP RUNNER] Searching for image for article: {finalArticle.Title}");
                    var imageUrl = await _imageFinder.FindImageUrlAsync(finalArticle.Title, ct);
                    finalArticle.ImageUrl = imageUrl;
                }

                var exists = await _reader.ExistsByUrlAsync(finalArticle.Url);
                if (exists)
                {
                    Console.WriteLine($"[APP RUNNER] Article already exists, skipping: {finalArticle.Url}");
                    continue;
                }

                try
                {
                    await _sink.InsertAsync(finalArticle);
                    createdCount++;
                    Console.WriteLine($"[APP RUNNER] Successfully created and saved article: {finalArticle.Title}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[APP RUNNER ERROR] Failed to save article: {ex.Message} (URL: {finalArticle.Url})");
                }
            }

            return createdCount;
        }
    }
}