using System.Globalization;
using MDS.Data.Context;
using MDS.Data.Repositories;
using MDS.Runner.Scraper.Services;
using MDS.Runner.Scraper.Scrapers.CraigsList;
using MDS.Runner.Scraper.Scrapers.OpAjuda;
using MeDeixaSaber.Core.Services;
using Microsoft.Extensions.Logging;

using var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("MDS.Runner.Scraper", LogLevel.Debug)
        .AddConsole(options => options.IncludeScopes = true);
});

var logger = loggerFactory.CreateLogger("MDS.Runner.Scraper");

static string? ArgEq(string[] args, string name)
    => args.FirstOrDefault(a => a.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

static bool HasFlag(string[] args, string flag)
    => args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

var wipeEq = ArgEq(args, "wipe");
var wipeDays = 0;
int.TryParse(wipeEq, out wipeDays);
var dateEq = ArgEq(args, "date");
var noUpload = HasFlag(args, "--no-upload");

var normalizationService = new TitleNormalizationService();
var filter = new ClassifiedsFilter(normalizationService);
var reader = new DefaultScrapedCsvReader();

var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "tcp:mds-sqlserver-eastus2-prod01.database.windows.net,1433";
var database = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "mds-sql-db-prod";
if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
{
    Console.WriteLine("SQL_SERVER/SQL_DATABASE ausentes. Pulo a persistência.");
    return;
}

var repoLogger = loggerFactory.CreateLogger<ClassifiedsRepository>();
var factory = new SqlConnectionFactory(server, database);
var repo = new ClassifiedsRepository(factory, normalizationService, repoLogger);

IStorageUploader? uploader =
    noUpload ? null : (HasFlag(args, "--local") ? new LocalUploader() : new BlobUploader("mdsprodstg04512", "scraped"));

var orchestrator = new ScrapeOrchestrator(
    new CraigsAdapter(),
    new OpAjudaAdapter(),
    repo,
    normalizationService,
    filter,
    reader,
    uploader,
    loggerFactory
);

DateTime targetDateUtc;
if (!string.IsNullOrWhiteSpace(dateEq) &&
    DateTime.TryParseExact(dateEq, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
    targetDateUtc = parsed.Date;
else
    targetDateUtc = DateTime.UtcNow.Date;

var handler = new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
};

var proxyUrl = Environment.GetEnvironmentVariable("CRAWL_HTTP_PROXY");
if (!string.IsNullOrWhiteSpace(proxyUrl))
{
    handler.Proxy = new System.Net.WebProxy(proxyUrl);
    handler.UseProxy = true;
}

using var http = new HttpClient(handler);

if (wipeDays > 0)
{
    var total = 0;
    for (var i = 0; i < wipeDays; i++)
        total += await orchestrator.RunForDateAsync(http, DateTime.UtcNow.Date.AddDays(-i), doUpload: uploader is not null);
    Console.WriteLine($"Wipe concluído. Dias: {wipeDays}. Inseridos: {total}");
    return;
}

await orchestrator.RunForDateAsync(http, targetDateUtc, doUpload: uploader is not null);
