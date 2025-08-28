using System.Net.Http;
using System.Threading.Tasks;
using MDS.Runner.Scraper.Services.Interfaces;
using MDS.Runner.Scraper.Scrapers;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Test.Orchestration;

public sealed class FakeScraper(string site, string fileToReturn) : IScraper
{
    public Task<ScrapeResult> RunAsync(HttpClient http, string dateStr, ILogger logger)
        => Task.FromResult(new ScrapeResult(site, dateStr, 0, 0, fileToReturn, null));
}