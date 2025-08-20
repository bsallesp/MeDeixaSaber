using System.Globalization;
using MDS.Data.Data;
using MDS.Data.Repositories;
using MDS.Scraper.Services;
using MDS.Scraper.Scrapers.AcheiUsa;
using MDS.Scraper.Scrapers.OpAjuda;

static string? ArgEq(string[] args, string name)
    => args.FirstOrDefault(a => a.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase))?.Split('=')[1];

static bool HasFlag(string[] args, string flag)
    => args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

static string NormalizeTitle(string? s)
{
    if (string.IsNullOrWhiteSpace(s)) return "";
    var t = s.Trim();

    var sb = new System.Text.StringBuilder(t.Length);
    bool prevSpace = false;
    foreach (var ch in t)
    {
        var c = char.IsWhiteSpace(ch) ? ' ' : ch;
        if (c == ' ')
        {
            if (prevSpace) continue;
            prevSpace = true;
        }
        else prevSpace = false;
        sb.Append(c);
    }

    var decomposed = sb.ToString().Normalize(System.Text.NormalizationForm.FormD);
    var noMarks = new System.Text.StringBuilder(decomposed.Length);
    foreach (var ch in decomposed)
        if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
            noMarks.Append(ch);

    return noMarks.ToString().Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
}

static async Task<int> RunForDateAsync(HttpClient http, DateTime dateUtc, bool doUpload, string[] args)
{
    var dateStr = dateUtc.ToString("yyyy-MM-dd");
    var r1 = await AcheiUsaScraper.RunAsync(http, dateStr);
    var r2 = await OpAjudaScraper.RunAsync(http, dateStr);
    var f1 = (string)((dynamic)r1).itemsFile;
    var f2 = (string)((dynamic)r2).itemsFile;

    if (doUpload)
    {
        IStorageUploader uploader = HasFlag(args, "--local") ? new LocalUploader() : new BlobUploader("mdsprodstg04512", "scraped");
        await uploader.SaveAsync("acheiusa", f1);
        await uploader.SaveAsync("opajuda", f2);
    }

    var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "tcp:mds-sqlserver-eastus2-prod01.database.windows.net,1433";
    var database = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "mds-sql-db-prod";
    if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database))
    {
        Console.WriteLine("SQL_SERVER/SQL_DATABASE ausentes. Pulo a persistência.");
        return 0;
    }

    var factory = new SqlConnectionFactory(server!, database!);
    var repo = new ClassifiedsRepository(factory);

    var list1 = ScrapedCsvReader.Load(f1);
    var list2 = ScrapedCsvReader.Load(f2);
    var merged = list1.Concat(list2);

    var existing = await repo.GetByDayAsync(dateUtc);
    var dbKeysByTitle = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (var c in existing)
        dbKeysByTitle.Add($"{c.PostDate:yyyy-MM-dd}|{NormalizeTitle(c.Title)}");

    var seenBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var toInsert = merged.Where(s =>
    {
        var key = $"{s.PostDate:yyyy-MM-dd}|{NormalizeTitle(s.Title)}";
        if (dbKeysByTitle.Contains(key)) return false;
        if (!seenBatch.Add(key)) return false;
        return true;
    }).ToList();

    Console.WriteLine($"Registros a inserir ({dateStr}): {toInsert.Count}");
    foreach (var c in toInsert) await repo.InsertAsync(c);
    Console.WriteLine($"Inseridos ({dateStr}): {toInsert.Count}");
    return toInsert.Count;
}

Console.WriteLine("Starting scrapers...VB00008");

var wipeEq = ArgEq(args, "wipe");
int wipeDays = 0;
int.TryParse(wipeEq, out wipeDays);
var dateEq = ArgEq(args, "date");
var noUpload = HasFlag(args, "--no-upload");

if (wipeDays > 0)
{
    using var http = new HttpClient();
    var total = 0;
    for (int i = 0; i < wipeDays; i++)
        total += await RunForDateAsync(http, DateTime.UtcNow.Date.AddDays(-i), false, args);
    Console.WriteLine($"Wipe concluído. Dias: {wipeDays}. Inseridos: {total}");
    return;
}

DateTime targetDateUtc;
if (!string.IsNullOrWhiteSpace(dateEq) &&
    DateTime.TryParseExact(dateEq, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
    targetDateUtc = parsed.Date;
else
    targetDateUtc = DateTime.UtcNow.Date;

using (var http = new HttpClient())
{
    await RunForDateAsync(http, targetDateUtc, !noUpload, args);
}
