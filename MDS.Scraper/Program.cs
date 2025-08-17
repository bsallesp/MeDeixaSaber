using System.Globalization;
using MDS.Data.Data;
using MDS.Data.Repositories;
using MDS.Scraper.Services;
using MDS.Scraper.Scrapers.AcheiUsa;
using MDS.Scraper.Scrapers.OpAjuda;

static string? GetArg(string[] args, string name)
    => args.FirstOrDefault(a => a.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

Console.WriteLine("Starting scrapers...VB00004");

var dateArg = GetArg(args, "date");
var daysArg = GetArg(args, "days");
DateTime targetDateUtc;

if (!string.IsNullOrWhiteSpace(dateArg) &&
    DateTime.TryParseExact(dateArg, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
{
    targetDateUtc = parsed.Date;
}
else if (!string.IsNullOrWhiteSpace(daysArg) && int.TryParse(daysArg, out var nDays))
{
    targetDateUtc = DateTime.UtcNow.Date.AddDays(-nDays);
}
else
{
    targetDateUtc = DateTime.UtcNow.Date;
}

using var http = new HttpClient();
var dateStr = targetDateUtc.ToString("yyyy-MM-dd");

var acheiUsaResult = await AcheiUsaScraper.RunAsync(http, dateStr);
var opAjudaResult = await OpAjudaScraper.RunAsync(http, dateStr);

IStorageUploader uploader = args.Any(a => a.Equals("--local", StringComparison.OrdinalIgnoreCase))
    ? new LocalUploader()
    : new BlobUploader("mdsprodstg04512", "scraped");

var acheiFile = (string)((dynamic)acheiUsaResult).itemsFile;
var opajudaFile = (string)((dynamic)opAjudaResult).itemsFile;

await uploader.SaveAsync("acheiusa", acheiFile);
await uploader.SaveAsync("opajuda", opajudaFile);

var server = Environment.GetEnvironmentVariable("SQL_SERVER");
var database = Environment.GetEnvironmentVariable("SQL_DATABASE");

if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(database))
{
    var factory = new SqlConnectionFactory(server!, database!);
    var repo = new ClassifiedsRepository(factory);
    var service = new DedupAndPersist(repo);

    var list1 = ScrapedCsvReader.Load(acheiFile);
    var list2 = ScrapedCsvReader.Load(opajudaFile);
    var merged = list1.Concat(list2);

    var inserted = await service.UpsertNewAsync(merged, targetDateUtc);
    Console.WriteLine($"Inserted: {inserted}");
}
else
{
    Console.WriteLine("SQL_SERVER/SQL_DATABASE não definidos. Pulo da persistência no banco.");
}
