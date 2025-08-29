using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Scrapers.CraigsList;

public static partial class CraigsScraper
{
    private static string Clean(string? s) => MyRegex().Replace(HtmlEntity.DeEntitize(s ?? ""), " ").Trim();

    private static string? ToIsoDate(string? dt)
    {
        if (string.IsNullOrWhiteSpace(dt)) return null;
        return DateTime.TryParse(dt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var d) ? d.ToString("yyyy-MM-dd") : null;
    }

    {
        var pattern = Environment.GetEnvironmentVariable("CRAIGS_LOCATION_SUFFIX_REGEX");
        if (string.IsNullOrWhiteSpace(pattern)) pattern = @",?\s*United\s+States$";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    });

    static IDictionary<string, string> LoadMap(string envName)
    {
        var raw = Environment.GetEnvironmentVariable(envName);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return map;
        try
        {
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(raw);
            if (json is not null) return new Dictionary<string, string>(json, StringComparer.OrdinalIgnoreCase);
        }
        catch {}
        foreach (var pair in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var kv = pair.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length == 2 && !string.IsNullOrWhiteSpace(kv[0])) map[kv[0]] = kv[1];
        }
        return map;
    }

    static readonly Lazy<IDictionary<string, string>> CityToDomain = new(() => LoadMap("CRAIGS_CITY_TO_DOMAIN"));
    static readonly Lazy<IDictionary<string, string>> DomainLabels  = new(() => LoadMap("CRAIGS_DOMAIN_LABELS"));

    static string LabelForDomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain)) return "";
        if (DomainLabels.Value.TryGetValue(subdomain, out var label) && !string.IsNullOrWhiteSpace(label)) return label;
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(subdomain.Replace('-', ' ').ToLowerInvariant());
    }

    static string CleanLocation(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var t = Clean(s);
        t = Regex.Replace(t, @"^\(|\)$", "");
        t = LocationSuffixRx.Value.Replace(t, "");
        return t.Trim();
    }

    private static List<(string url, string? whenIso, string? location, string refId)> ExtractLinks(string html, Uri baseUri, ILogger logger)
    {
        var results = new List<(string, string?, string?, string)>();
        if (string.IsNullOrWhiteSpace(html)) return results;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        foreach (var row in rows)
        {
            try
            {
                var a = row.SelectSingleNode(
                    ".//a[contains(@class,'result-title') or contains(@class,'posting-title') or contains(@class,'titlestring') or @href]");
                var href = a?.GetAttributeValue("href", "").Trim() ?? "";
                if (string.IsNullOrWhiteSpace(href)) continue;
                if (!Uri.TryCreate(baseUri, href, out var abs)) continue;

                var time = row.SelectSingleNode(".//time[@datetime] | .//*[@datetime][self::time]");
                var whenIso = time?.GetAttributeValue("datetime", "");

                var hood = CleanLocation(row.SelectSingleNode(".//span[contains(@class,'result-hood')]")?.InnerText ?? "");
                if (string.IsNullOrWhiteSpace(hood))
                {
                    var sub = baseUri.Host.Split('.')[0];
                    hood = LabelForDomain(sub);
                }

                var refId = row.GetAttributeValue("data-pid", "");
                if (string.IsNullOrWhiteSpace(refId))
                {
                    var m = Regex.Match(abs.ToString(), @"(\d{9,12})\.html");
                    refId = m.Success ? m.Groups[1].Value : "";
                }

                results.Add((abs.ToString(), whenIso, string.IsNullOrWhiteSpace(hood) ? null : hood, refId));
            }
            catch {}
        }

        return results;
    }

    private static (string title, string description) ParseItem(string html, ILogger logger)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title = Clean(
            doc.DocumentNode.SelectSingleNode("//span[@id='titletextonly']")?.InnerText
            ?? doc.DocumentNode.SelectSingleNode("//h1[contains(@class,'postingtitle')]")?.InnerText
            ?? doc.DocumentNode.SelectSingleNode("//h1|//title")?.InnerText
            ?? ""
        );

        var body = doc.DocumentNode.SelectSingleNode(
            "//section[@id='postingbody'] | //div[@id='postingbody'] | //section[contains(@class,'post-body')]");
        var description = Clean(body?.InnerText ?? doc.DocumentNode.InnerText);

        return (title, description);
    }

    public static async Task<ScrapeResult> RunAsync(HttpClient http, string today, ILogger logger)
    {
        if (!http.DefaultRequestHeaders.UserAgent.Any())
        {
            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36");
            http.DefaultRequestHeaders.Accept.ParseAdd(
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            http.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        }

        var pauseMs = 1500;
        var pauseRaw = Environment.GetEnvironmentVariable("SCRAPER_PAUSE_MS");
        if (!string.IsNullOrWhiteSpace(pauseRaw) && int.TryParse(pauseRaw, out var parsed) && parsed >= 0) pauseMs = parsed;
        var jitter = Math.Min(pauseMs / 3, 250);

        static void SetOrReplaceHeader(System.Net.Http.Headers.HttpRequestHeaders h, string name, string? value)
        {
            if (h.Contains(name)) h.Remove(name);
            if (!string.IsNullOrWhiteSpace(value)) h.TryAddWithoutValidation(name, value);
        }

        async Task<string> FetchPageAsync(HttpClient h, string url, string? referer, ILogger log)
        {
            await GlobalThrottle.WaitAsync(pauseMs, jitter);
            SetOrReplaceHeader(h.DefaultRequestHeaders, "Referer", referer);
            using var resp = await h.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode) return "";
            return await resp.Content.ReadAsStringAsync();
        }

        async Task<string> FetchItemAsync(HttpClient h, string url, string? referer, ILogger log)
        {
            await GlobalThrottle.WaitAsync(pauseMs, jitter);
            SetOrReplaceHeader(h.DefaultRequestHeaders, "Referer", referer);
            using var resp = await h.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!resp.IsSuccessStatusCode) return "";
            return await resp.Content.ReadAsStringAsync();
        }

        static IEnumerable<string> ResolveDomainsFromEnv()
        {
            var csv = Environment.GetEnvironmentVariable("CRAIGS_TARGET_CITIES");
            if (string.IsNullOrWhiteSpace(csv)) return new[] { "miami" };

            var cities = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(c => c.ToLowerInvariant());

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var city in cities)
            {
                if (CityToDomain.Value.TryGetValue(city, out var domain) && !string.IsNullOrWhiteSpace(domain))
                {
                    domains.Add(domain);
                }
                else
                {
                    var fallback = Regex.Replace(city, @"\s+", "");
                    if (!string.IsNullOrWhiteSpace(fallback)) domains.Add(fallback);
                }
            }

            return domains.Count > 0 ? domains : new[] { "miami" };
        }

        static Func<int, string> BaseUrlBuilder(string domain) => (offset) => $"https://{domain}.craigslist.org/search/jjj?sort=date&s={offset}";

        var outDir = ScraperIO.GetOutputDir();
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        var itemsFile = Path.Combine(outDir, $"craigslist-items-{stamp}.csv");

        var dataLock = new SemaphoreSlim(1, 1);
        await ScraperIO.AppendToFile(itemsFile,
            ScraperIO.Csv("captured_at_utc", "url", "title", "ref_id", "location", "when", "post_date", "phone",
                "state", "description"), dataLock);

        var totalItems = 0;
        var pages = 0;
        var seenRefs = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var seenUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        var targetDomains = ResolveDomainsFromEnv().ToList();

        foreach (var domain in targetDomains)
        {
            var BuildUrl = BaseUrlBuilder(domain);

            for (var offset = 0; offset <= 1200; offset += 120)
            {
                var url = BuildUrl(offset);
                string html;
                try
                {
                    html = await FetchPageAsync(http, url, referer: null, logger);
                }
                catch
                {
                    break;
                }

                var links = ExtractLinks(html, new Uri(url), logger);
                if (links.Count == 0) break;

                var sem = new SemaphoreSlim(6);
                var pageNew = 0;
                var stopEarly = 0;

                var tasks = links.Select(async link =>
                {
                    if (!string.IsNullOrWhiteSpace(link.refId))
                    {
                        if (!seenRefs.TryAdd(link.refId, 0)) return;
                    }
                    else
                    {
                        if (!seenUrls.TryAdd(link.url, 0)) return;
                    }

                    var listIso = ToIsoDate(link.whenIso);
                    if (listIso != null && listIso != today)
                    {
                        if (StringComparer.Ordinal.Compare(listIso, today) < 0) Interlocked.Exchange(ref stopEarly, 1);
                        return;
                    }

                    await sem.WaitAsync();
                    try
                    {
                        string itemHtml;
                        try
                        {
                            itemHtml = await FetchItemAsync(http, link.url, referer: url, logger);
                            if (string.IsNullOrWhiteSpace(itemHtml))
                            {
                                logger.LogWarning("Item com status não-2xx (pulado): {Url}", link.url);
                                return;
                            }
                        }
                        catch
                        {
                            return;
                        }

                        var (title, description) = ParseItem(itemHtml, logger);

                        string? postDate = null;
                        try
                        {
                            var d = new HtmlDocument();
                            d.LoadHtml(itemHtml);
                            var timeNode =
                                d.DocumentNode.SelectSingleNode(
                                    "//time[@datetime] | //p[@class='postinginfo']/time[@datetime]");
                            postDate = ToIsoDate(timeNode?.GetAttributeValue("datetime", ""));
                        }
                        catch {}

                        var finalPostDate = postDate ?? listIso;
                        if (finalPostDate != today) return;

                        var line = ScraperIO.Csv(
                            ScraperIO.NowIso(),
                            link.url,
                            title,
                            link.refId,
                            link.location ?? "",
                            "",
                            finalPostDate ?? "",
                            "",
                            "",
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

                if (pauseMs > 0) await Task.Delay(pauseMs);
                if (pageNew == 0 || Volatile.Read(ref stopEarly) == 1) break;
            }
        }

        return new ScrapeResult("craigslist", today, pages, totalItems, itemsFile, null);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}
