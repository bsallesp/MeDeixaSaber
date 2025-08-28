using System.Net.Http;
using System.Threading.Tasks;
using MDS.Runner.Scraper.Scrapers;
using Microsoft.Extensions.Logging;

namespace MDS.Runner.Scraper.Services.Interfaces;

public interface IScraper
{
    Task<ScrapeResult> RunAsync(HttpClient http, string dateStr, ILogger logger);
}