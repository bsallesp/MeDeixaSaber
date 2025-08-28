using System.Net.Http;
using System.Threading.Tasks;
using MDS.Runner.Scraper.Scrapers;
using MDS.Runner.Scraper.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Scrapers.CraigsList;

public sealed class CraigsAdapter : IScraper
{
    public Task<ScrapeResult> RunAsync(HttpClient http, string dateStr, ILogger logger)
        => CraigsScraper.RunAsync(http, dateStr, logger);
}