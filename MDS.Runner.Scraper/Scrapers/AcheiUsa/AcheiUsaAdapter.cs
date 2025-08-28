using System.Net.Http;
using System.Threading.Tasks;
using MDS.Runner.Scraper.Scrapers;
using MDS.Runner.Scraper.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Scrapers.AcheiUsa;

public sealed class AcheiUsaAdapter : IScraper
{
    public async Task<ScrapeResult> RunAsync(HttpClient http, string dateStr, ILogger logger)
    {
        var r = await AcheiUsaScraper.RunAsync(http, dateStr, logger);
        return new ScrapeResult(r.Site, r.Date, r.Pages, r.TotalItems, r.ItemsFile, r.LogFile);
    }
}