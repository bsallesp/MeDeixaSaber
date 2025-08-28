using System.Globalization;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using MDS.Data.Repositories.Interfaces;
using MDS.Runner.Scraper.Interfaces;
using MDS.Runner.Scraper.Services.Interfaces;
using MDS.Runner.Scraper.Scrapers;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Services;

public sealed class ScrapeOrchestrator(
    IScraper scraper1,
    IScraper scraper2,
    IClassifiedsRepository repo,
    ITitleNormalizationService norm,
    IClassifiedsFilter filter,
    IScrapedCsvReader reader,
    IStorageUploader? uploader,
    ILoggerFactory loggerFactory)
{
    private readonly ILogger<ScrapeOrchestrator> _logger = loggerFactory.CreateLogger<ScrapeOrchestrator>();

    public async Task<int> RunForDateAsync(HttpClient http, DateTime dateUtc, bool doUpload)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        _ = scraper1 ?? throw new ArgumentNullException(nameof(scraper1));
        _ = scraper2 ?? throw new ArgumentNullException(nameof(scraper2));
        _ = repo ?? throw new ArgumentNullException(nameof(repo));
        _ = filter ?? throw new ArgumentNullException(nameof(filter));
        _ = reader ?? throw new ArgumentNullException(nameof(reader));
        _ = norm ?? throw new ArgumentNullException(nameof(norm));

        var dateStr = dateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        _logger.LogInformation("Iniciando execução do ScrapeOrchestrator para a data: {Date}", dateStr);

        var r1 = await scraper1.RunAsync(http, dateStr, loggerFactory.CreateLogger(scraper1.GetType().Name));
        var r2 = await scraper2.RunAsync(http, dateStr, loggerFactory.CreateLogger(scraper2.GetType().Name));

        if (doUpload && uploader is not null)
        {
            _logger.LogInformation("Iniciando upload para o storage...");
            var uploaderLogger = loggerFactory.CreateLogger(uploader.GetType().Name);
            await uploader.SaveAsync(r1.Site, r1.ItemsFile, uploaderLogger);
            await uploader.SaveAsync(r2.Site, r2.ItemsFile, uploaderLogger);
            _logger.LogInformation("Upload concluído.");
        }

        var list1 = reader.Load(r1.ItemsFile) ?? Enumerable.Empty<Classified>();
        var list2 = reader.Load(r2.ItemsFile) ?? Enumerable.Empty<Classified>();
        var merged = list1.Concat(list2);

        var existing = await repo.GetByDayAsync(dateUtc);
        var toInsert = filter.Filter(merged, existing).ToList();

        foreach (var c in toInsert)
            await repo.InsertAsync(c);

        return toInsert.Count;
    }
}
