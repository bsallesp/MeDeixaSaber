using System.Globalization;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using MDS.Data.Repositories;
using MDS.Data.Repositories.Interfaces;

namespace MDS.Runner.Scraper.Services;

public interface IScraper
{
    Task<string> RunAsync(HttpClient http, string dateStr);
}

public interface IScrapedCsvReader
{
    IEnumerable<Classified> Load(string path);
}

public sealed class DefaultScrapedCsvReader : IScrapedCsvReader
{
    public IEnumerable<Classified> Load(string path) => ScrapedCsvReader.Load(path);
}

public sealed class ScrapeOrchestrator(
    IScraper scraper1,
    IScraper scraper2,
    IClassifiedsRepository repo,
    ITitleNormalizationService norm,
    IClassifiedsFilter filter,
    IScrapedCsvReader reader,
    IStorageUploader? uploader)
{
    public async Task<int> RunForDateAsync(HttpClient http, DateTime dateUtc, bool doUpload)
    {
        if (http is null) throw new ArgumentNullException(nameof(http));
        if (scraper1 is null) throw new ArgumentNullException(nameof(scraper1));
        if (scraper2 is null) throw new ArgumentNullException(nameof(scraper2));
        if (repo is null) throw new ArgumentNullException(nameof(repo));
        if (filter is null) throw new ArgumentNullException(nameof(filter));
        if (reader is null) throw new ArgumentNullException(nameof(reader));
        _ = norm ?? throw new ArgumentNullException(nameof(norm));

        var dateStr = dateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        var f1 = await scraper1.RunAsync(http, dateStr);
        var f2 = await scraper2.RunAsync(http, dateStr);

        if (doUpload && uploader is not null)
        {
            await uploader.SaveAsync("acheiusa", f1);
            await uploader.SaveAsync("opajuda", f2);
        }

        var list1 = reader.Load(f1) ?? Enumerable.Empty<Classified>();
        var list2 = reader.Load(f2) ?? Enumerable.Empty<Classified>();
        var merged = list1.Concat(list2);

        var existing = await repo.GetByDayAsync(dateUtc);
        var toInsert = filter.Filter(merged, existing).ToList();

        foreach (var c in toInsert)
            await repo.InsertAsync(c);

        return toInsert.Count;
    }
}
