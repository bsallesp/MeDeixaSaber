using MDS.Runner.Scraper.Services;

namespace MDS.Runner.Scraper.Scrapers.OpAjuda;

public sealed class OpAjudaAdapter : IScraper
{
    public async Task<string> RunAsync(HttpClient http, string dateStr)
    {
        var r = await OpAjudaScraper.RunAsync(http, dateStr);
        return (string)((dynamic)r).itemsFile;
    }
}