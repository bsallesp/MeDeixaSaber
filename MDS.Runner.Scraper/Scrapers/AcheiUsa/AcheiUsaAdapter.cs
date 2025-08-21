using MDS.Runner.Scraper.Services;

namespace MDS.Runner.Scraper.Scrapers.AcheiUsa;

public sealed class AcheiUsaAdapter : IScraper
{
    public async Task<string> RunAsync(HttpClient http, string dateStr)
    {
        var r = await AcheiUsaScraper.RunAsync(http, dateStr);
        return (string)((dynamic)r).itemsFile;
    }
}