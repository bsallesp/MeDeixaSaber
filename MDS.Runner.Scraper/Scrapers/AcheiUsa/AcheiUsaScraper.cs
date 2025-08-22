using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Scrapers.AcheiUsa;

public sealed record ScraperResult(
    string Site,
    string Date,
    int Pages,
    int TotalItems,
    string ItemsFile,
    string LogFile
);

public static class AcheiUsaScraper
{
    static string Clean(string? s) =>
        Regex.Replace(HtmlEntity.DeEntitize(s ?? ""), @"\s+", " ").Trim();

    static string? DerivePostDateFromWhen(string? whenText, DateTime? todayUtc = null)
    {
        if (string.IsNullOrWhiteSpace(whenText)) return null;
        todayUtc ??= DateTime.UtcNow;
        var mDate = Regex.Match(whenText, @"(?i)Data\s*:\s*(\d{2}/\d{2}/\d{4})");
        if (mDate.Success &&
            DateTime.TryParseExact(mDate.Groups[1].Value, "dd/MM/yyyy", new CultureInfo("pt-BR"),
                DateTimeStyles.AssumeLocal, out var dt))
            return dt.ToString("yyyy-MM-dd");

        var mRel = Regex.Match(whenText,
            @"(?i)\b(\d+)\s+(hora|horas|dia|dias|semana|semanas|m[eê]s|meses|ano|anos)\s+atr[aá]s\b");
        if (!mRel.Success) return null;

        var qty = int.Parse(mRel.Groups[1].Value);
        var unit = mRel.Groups[2].Value.ToLowerInvariant();
        var baseDate = todayUtc.Value;
        if (unit.StartsWith("hora")) return baseDate.AddHours(-qty).ToString("yyyy-MM-dd");
        if (unit.StartsWith("dia")) return baseDate.AddDays(-qty).ToString("yyyy-MM-dd");
        if (unit.StartsWith("semana")) return baseDate.AddDays(-7 * qty).ToString("yyyy-MM-dd");
        if (unit.StartsWith("m")) return baseDate.AddMonths(-qty).ToString("yyyy-MM-dd");
        if (unit.StartsWith("ano")) return baseDate.AddYears(-qty).ToString("yyyy-MM-dd");
        return null;
    }

    static string NormalizePhone(string input)
    {
        var digits = Regex.Replace(input ?? "", @"\D", "");
        if (digits.Length == 11 && digits.StartsWith("1")) digits = digits[1..];
        if (digits.Length == 10) return $"({digits[..3]}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
        return string.IsNullOrWhiteSpace(input) ? "" : Clean(input);
    }

    static string ExtractTitle(HtmlDocument doc)
    {
        var og = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title' or @name='og:title']")
            ?.GetAttributeValue("content", null);
        if (!string.IsNullOrWhiteSpace(og)) return Clean(og);

        var pageTitle = Clean(doc.DocumentNode.SelectSingleNode("//title")?.InnerText ?? "");
        if (!string.IsNullOrWhiteSpace(pageTitle))
        {
            pageTitle = Regex.Replace(pageTitle, @"\s*[-|–]\s*Classificados\s*AcheiUSA.*$", "",
                RegexOptions.IgnoreCase);
            if (!string.IsNullOrWhiteSpace(pageTitle)) return pageTitle;
        }

        var h = doc.DocumentNode.SelectSingleNode(
                    "//h1[contains(@class,'title') or contains(@class,'entry-title') or contains(@class,'ad-title') or contains(@class,'single')]")
                ?? doc.DocumentNode.SelectSingleNode("//h1|//h2|//h3");
        return Clean(h?.InnerText ?? "");
    }

    static HtmlNode FindMain(HtmlDocument doc)
    {
        var xpaths = new[]
        {
            "//*[contains(@class,'single-ad') or contains(@class,'ad-single') or (contains(@class,'classified') and contains(@class,'single'))]",
            "//*[contains(@class,'entry-content') or contains(@class,'post-content') or contains(@class,'content')]",
            "//article", "//main", "//*[@id='content' or @id='primary']",
        };
        foreach (var xp in xpaths)
        {
            var n = doc.DocumentNode.SelectSingleNode(xp);
            if (n != null) return n;
        }
        return doc.DocumentNode;
    }

    static void DropGarbageNodes(HtmlNode root)
    {
        var xp = ".//*[self::nav or self::footer or self::form or self::aside]";
        var nodes = root.SelectNodes(xp);
        if (nodes == null) return;
        foreach (var n in nodes) n.Remove();
    }

    static IEnumerable<string> ExtractPhones(HtmlNode scope)
    {
        var telLinks = scope.SelectNodes(".//a[starts-with(@href,'tel:')]")
            ?.Select(a => a.GetAttributeValue("href", "").Replace("tel:", "")) ?? Enumerable.Empty<string>();
        var inline = Regex.Matches(scope.InnerText, @"(\+1[\s\-\.]?)?\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}");
        return telLinks.Concat(inline.Select(m => m.Value))
            .Select(NormalizePhone)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct();
    }

    static string? ParsePostDateFromText(string text)
    {
        var m = Regex.Match(text, @"\(\s*Data:\s*(\d{2}/\d{2}/\d{4})\s*\)", RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        if (DateTime.TryParseExact(m.Groups[1].Value, "dd/MM/yyyy", new CultureInfo("pt-BR"),
                DateTimeStyles.AssumeLocal, out var dt))
            return dt.ToString("yyyy-MM-dd");
        return m.Groups[1].Value;
    }

    static (string title, string description, string refId, string? phone, string? location, string? when,
        string? postDate) ParseItem(string url, string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var title = ExtractTitle(doc);
        var refId = Regex.Match(url, @"/ad/(\d+)/", RegexOptions.IgnoreCase).Groups[1].Value;
        var main = FindMain(doc);
        DropGarbageNodes(main);
        var description = Clean(main.InnerText);
        var postDate = ParsePostDateFromText(doc.DocumentNode.InnerText);
        var phone = ExtractPhones(main).FirstOrDefault();
        return (title, description, refId, phone, null, null, postDate);
    }

    static List<(string url, string? location, string? when)> ExtractLinks(string html, Uri baseUri)
    {
        var results = new List<(string, string?, string?)>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var anchors = doc.DocumentNode.SelectNodes("//a[contains(@href,'/ad/')]");
        if (anchors == null) return results;
        foreach (var a in anchors)
        {
            var href = a.GetAttributeValue("href", "");
            if (!Uri.TryCreate(baseUri, href, out var absUri)) continue;
            results.Add((absUri.ToString(), null, null));
        }
        return results;
    }

    public static async Task<ScraperResult> RunAsync(HttpClient http, string today)
    {
        var outDir = ScraperIO.GetOutputDir();
        var stamp = $"{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss-fffffff}-{Guid.NewGuid():N}";
        var itemsCsv = Path.Combine(outDir, $"acheiusa-items-{stamp}.csv");
        var logFile = Path.Combine(outDir, $"acheiusa-log-{stamp}.txt");

        var logLock = new SemaphoreSlim(1, 1);
        var dataLock = new SemaphoreSlim(1, 1);

        await ScraperIO.AppendToFile(itemsCsv,
            string.Join(",", "captured_at_utc", "url", "title", "ref_id", "location", "when", "post_date", "phone",
                "state", "description"),
            dataLock);

        await ScraperIO.AppendToFile(logFile, $"{ScraperIO.NowIso()}\tRUN_START", logLock);

        var totalItems = 0;
        var pages = 0;

        var url = "https://classificados.acheiusa.com/category/12/emprego/";
        var html = await http.GetStringAsync(url);
        var links = ExtractLinks(html, new Uri(url));

        foreach (var link in links)
        {
            var itemHtml = await http.GetStringAsync(link.url);
            var (title, description, refId, phone, _, _, postDate) = ParseItem(link.url, itemHtml);
            var finalPostDate = postDate ?? DerivePostDateFromWhen(string.Empty);
            if (finalPostDate != today) continue;

            var line = string.Join(",",
                ScraperIO.CsvEscape(ScraperIO.NowIso()),
                ScraperIO.CsvEscape(link.url),
                ScraperIO.CsvEscape(title),
                ScraperIO.CsvEscape(refId),
                ScraperIO.CsvEscape(""),
                ScraperIO.CsvEscape(""),
                ScraperIO.CsvEscape(finalPostDate),
                ScraperIO.CsvEscape(phone),
                ScraperIO.CsvEscape(""),
                ScraperIO.CsvEscape(description));

            await ScraperIO.AppendToFile(itemsCsv, line, dataLock);
            totalItems++;
        }

        pages++;

        await ScraperIO.AppendToFile(logFile, $"{ScraperIO.NowIso()}\tRUN_SUMMARY\tpages={pages}\ttotalItems={totalItems}", logLock);

        return new ScraperResult("acheiusa", today, pages, totalItems, itemsCsv, logFile);
    }
}
