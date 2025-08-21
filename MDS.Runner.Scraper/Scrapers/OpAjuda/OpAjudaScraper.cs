using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Scrapers.OpAjuda;

public static class OpAjudaScraper
{
    private static string Clean(string s) =>
        Regex.Replace(HtmlEntity.DeEntitize(s ?? ""), @"\s+", " ").Trim();

    private static string? ToIsoDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = s.Trim();
        if (DateTime.TryParseExact(t, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d.ToString("yyyy-MM-dd");
        if (DateTime.TryParseExact(t, "dd/MM/yyyy", new CultureInfo("pt-BR"), DateTimeStyles.None, out d))
            return d.ToString("yyyy-MM-dd");
        if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d.ToString("yyyy-MM-dd");
        return t;
    }

    private static (string title, string state, string description, string date) ParseItem(HtmlDocument doc)
    {
        var box = doc?.DocumentNode?.SelectSingleNode("//div[contains(@class,'rounded-md') and contains(@class,'p-4')]");
        var title = Clean(box?.SelectSingleNode(".//li[contains(@class,'text-2xl')]")?.InnerText ?? "");
        var state = Clean(box?.SelectSingleNode(".//li[contains(@class,'uppercase')][2]")?.InnerText ?? "");
        var description = Clean(box?.SelectSingleNode(".//li[p]")?.InnerText ?? "");
        var dateText = Clean(box?.SelectSingleNode(".//li[contains(.,'Ativo desde o dia')]")?.InnerText ?? "");
        var m = Regex.Match(dateText, @"\b\d{2}/\d{2}/\d{4}\b");
        var date = m.Success ? m.Value : "";
        return (title, state, description, date);
    }

    private static List<string> ExtractLinks(string html, Uri baseUri)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(html)) return result;
        var doc = new HtmlDocument();
        try { doc.LoadHtml(html); } catch { return result; }
        var anchors = doc.DocumentNode?.SelectNodes("//a") ?? new HtmlNodeCollection(null);
        foreach (var a in anchors)
        {
            var href = a?.GetAttributeValue("href", "")?.Trim();
            if (string.IsNullOrWhiteSpace(href)) continue;
            if (href.StartsWith("#") || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) continue;
            if (!href.Contains("/classified-show/", StringComparison.OrdinalIgnoreCase)) continue;
            if (Uri.TryCreate(baseUri, href, out var abs))
                result.Add(abs.ToString());
            else
                result.Add(href);
        }
        return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string RefIdFromUrl(string u)
    {
        var m = Regex.Match(u ?? "", @"/classified-show/(\d+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : "";
    }

    public static async Task<object> RunAsync(HttpClient http, string today)
{
    var categories = new[]
    {
        "https://oportunidadeeajuda.com/classified/EMPREGO",
        "https://oportunidadeeajuda.com/classified/ALUGUEIS",
        "https://oportunidadeeajuda.com/classified/ZONALIVRE",
        "https://oportunidadeeajuda.com/classified/CARROS",
        "https://oportunidadeeajuda.com/classified/SERVICOS",
        "https://oportunidadeeajuda.com/classified/DOACOES"
    };

    var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
    var outDir = ScraperIO.GetOutputDir();
    var itemsFile = Path.Combine(outDir, $"opajuda-items-{stamp}.csv");
    var logFile   = Path.Combine(outDir, $"opajuda-log-{stamp}.txt");
    var logLock  = new SemaphoreSlim(1, 1);
    var dataLock = new SemaphoreSlim(1, 1);
    var totalItems = 0;
    var pages = 0;

    await ScraperIO.AppendToFile(itemsFile,
        ScraperIO.Csv("captured_at_utc","url","title","ref_id","location","when","post_date","phone","state","description"),
        dataLock);

    var seenRefIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var seenUrls   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    string PageUrl(string baseUrl, int p)
    {
        var b = baseUrl.TrimEnd('/');
        if (p == 1) return b + "/";
        return b.Contains("?") ? $"{b}&page={p}" : $"{b}?page={p}";
    }

    foreach (var cat in categories)
    {
        int emptyStreak = 0;

        for (var page = 1; page <= 300; page++)
        {
            var url = PageUrl(cat, page);
            string html;
            try
            {
                await ScraperIO.AppendToFile(logFile, $"{ScraperIO.NowIso()}\tPAGE_START\t{url}", logLock);
                html = await http.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                await ScraperIO.AppendToFile(logFile, $"{ScraperIO.NowIso()}\tPAGE_FAIL\t{url}\t{ex.Message}", logLock);
                break;
            }

            var links = ExtractLinks(html, new Uri(url));
            if (links.Count == 0)
            {
                emptyStreak++;
                if (emptyStreak >= 2) break;
                continue;
            }

            var sem = new SemaphoreSlim(6);
            var pageNew = 0;

            var tasks = links.Select(async link =>
            {
                var refFromLink = RefIdFromUrl(link);
                if (!string.IsNullOrWhiteSpace(refFromLink) && !seenRefIds.Add(refFromLink)) return;
                if (string.IsNullOrWhiteSpace(refFromLink) && !seenUrls.Add(link)) return;

                await sem.WaitAsync();
                try
                {
                    string itemHtml;
                    try { itemHtml = await http.GetStringAsync(link); }
                    catch { return; }

                    var d = new HtmlDocument();
                    d.LoadHtml(itemHtml);
                    var (title, state, description, date) = ParseItem(d);

                    var postDate = ToIsoDate(date);
                    if (postDate != today) return; // <<< só pega o dia atual

                    var line = ScraperIO.Csv(
                        ScraperIO.NowIso(),
                        link,
                        title,
                        refFromLink,
                        "",
                        "",
                        postDate ?? "",
                        "",
                        state,
                        description
                    );

                    await ScraperIO.AppendToFile(itemsFile, line, dataLock);

                    Interlocked.Increment(ref totalItems);
                    Interlocked.Increment(ref pageNew);
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(tasks);
            pages++;

            if (pageNew == 0)
            {
                emptyStreak++;
                if (emptyStreak >= 1) break; // <<< encerra categoria quando não tem nenhum do dia
            }
            else
            {
                emptyStreak = 0;
            }
        }
    }

    return new { site = "opajuda", date = today, pages, totalItems, itemsFile, logFile };
}

}
