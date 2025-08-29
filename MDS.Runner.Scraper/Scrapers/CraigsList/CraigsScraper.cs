using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Scrapers.CraigsList;

public static partial class CraigsScraper
{
    private static string Clean(string? s) =>
        MyRegex().Replace(HtmlEntity.DeEntitize(s ?? ""), " ").Trim();

    private static string? ToIsoDate(string? dt)
    {
        if (string.IsNullOrWhiteSpace(dt)) return null;
        return DateTime.TryParse(dt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var d)
            ? d.ToString("yyyy-MM-dd")
            : null;
    }

    private static List<(string url, string? whenIso, string? location, string refId)> ExtractLinks(string html,
        Uri baseUri, ILogger logger)
    {
        var results = new List<(string, string?, string?, string)>();
        if (string.IsNullOrWhiteSpace(html))
        {
            logger.LogWarning("HTML de entrada para extração de links estava vazio ou nulo.");
            return results;
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rows = doc.DocumentNode.SelectNodes(
            "//li[contains(@class,'result-row')] | //li[contains(@class,'cl-static-search-result')]");
        if (rows == null || rows.Count == 0)
        {
            logger.LogWarning("Nenhuma linha (row) de resultado encontrada no HTML.");
            return results;
        }

        logger.LogDebug("Extraindo links de {RowCount} linhas.", rows.Count);

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

                var hood = Clean(row.SelectSingleNode(".//span[contains(@class,'result-hood')]")?.InnerText ?? "");
                hood = Regex.Replace(hood, @"^\(|\)$", "");

                var refId = row.GetAttributeValue("data-pid", "");
                if (string.IsNullOrWhiteSpace(refId))
                {
                    var m = Regex.Match(abs.ToString(), @"(\d{9,12})\.html");
                    refId = m.Success ? m.Groups[1].Value : "";
                }

                results.Add((abs.ToString(), whenIso, string.IsNullOrWhiteSpace(hood) ? null : hood, refId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao extrair link de uma linha (row).");
            }
        }

        logger.LogDebug("Extração de links concluída. Encontrados {ResultCount} links.", results.Count);
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

        logger.LogTrace("Item parsado: Título com {TitleLength} chars, Descrição com {DescLength} chars.", title.Length,
            description.Length);
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
        if (!string.IsNullOrWhiteSpace(pauseRaw) && int.TryParse(pauseRaw, out var parsed) && parsed >= 0)
            pauseMs = parsed;
        var jitter = Math.Min(pauseMs / 3, 250);
        logger.LogInformation("Craigslist: pause entre requests = {PauseMs}ms (jitter ±{Jitter}ms).", pauseMs, jitter);

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
            if (string.IsNullOrWhiteSpace(csv))
                return new[] { "miami" };

            var cities = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.ToLowerInvariant())
                .ToList();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["miami"] = "miami", ["fort lauderdale"] = "miami", ["pompano beach"] = "miami",
                ["deerfield beach"] = "miami", ["boca raton"] = "miami", ["west palm beach"] = "miami",
                ["orlando"] = "orlando", ["boston"] = "boston", ["framingham"] = "boston",
                ["somerville"] = "boston", ["everett"] = "boston", ["malden"] = "boston",
                ["marlborough"] = "boston", ["newark"] = "newjersey", ["elizabeth"] = "newjersey",
                ["new york"] = "newyork", ["mount vernon"] = "newyork", ["danbury"] = "newyork",
                ["bridgeport"] = "newyork", ["hyannis"] = "capecod", ["barnstable"] = "capecod",
            };

            var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var city in cities)
            {
                if (map.TryGetValue(city, out var domain))
                    domains.Add(domain);
                else
                {
                    var fallback = Regex.Replace(city, @"\s+", "");
                    if (!string.IsNullOrWhiteSpace(fallback))
                        domains.Add(fallback);
                }
            }

            return domains.Count > 0 ? domains : new[] { "miami" };
        }

        static Func<int, string> BaseUrlBuilder(string domain)
            => (offset) => $"https://{domain}.craigslist.org/search/jjj?sort=date&s={offset}";

        var outDir = ScraperIO.GetOutputDir();
        var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        var itemsFile = Path.Combine(outDir, $"craigslist-items-{stamp}.csv");

        var dataLock = new SemaphoreSlim(1, 1);
        await ScraperIO.AppendToFile(itemsFile,
            ScraperIO.Csv("captured_at_utc", "url", "title", "ref_id", "location", "when", "post_date", "phone",
                "state", "description"), dataLock);

        logger.LogInformation("Iniciando run do Craigslist para a data {Today}. Arquivo de saída: {ItemsFile}", today,
            itemsFile);

        var totalItems = 0;
        var pages = 0;
        var seenRefs = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var seenUrls = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        var targetDomains = ResolveDomainsFromEnv().ToList();
        logger.LogInformation("Domínios alvo: {Domains}", string.Join(",", targetDomains));

        foreach (var domain in targetDomains)
        {
            var BuildUrl = BaseUrlBuilder(domain);
            logger.LogInformation("Iniciando scraping para o domínio: {Domain}", domain);

            for (var offset = 0; offset <= 1200; offset += 120)
            {
                var url = BuildUrl(offset);
                string html;
                try
                {
                    logger.LogInformation("Buscando página com headers de browser: {Url}", url);
                    html = await FetchPageAsync(http, url, referer: null, logger);
                    if (string.IsNullOrWhiteSpace(html))
                    {
                        logger.LogWarning("Página sem conteúdo ou com status não-2xx: {Url}", url);
                        break;
                    }

                    logger.LogDebug("Página buscada com sucesso: {Url}", url);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falha ao buscar página: {Url}", url);
                    break;
                }

                var links = ExtractLinks(html, new Uri(url), logger);
                if (links.Count == 0)
                {
                    logger.LogWarning("Página retornou sem links: {Url}", url);
                    break;
                }

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
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Falha ao buscar item: {Url}", link.url);
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
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Erro ao extrair data do post para {Url}", link.url);
                        }

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
                        logger.LogInformation("Item salvo: {Url} (RefId: {RefId})", link.url,
                            string.IsNullOrWhiteSpace(link.refId) ? "-" : link.refId);
                    }
                    finally
                    {
                        sem.Release();
                    }
                });

                await Task.WhenAll(tasks);
                pages++;

                logger.LogInformation("Resumo da página (domínio {Domain}, offset {Offset}): {NewItems} itens novos.",
                    domain, offset, pageNew);

                if (pauseMs > 0) await Task.Delay(pauseMs);

                if (pageNew == 0 || Volatile.Read(ref stopEarly) == 1) break;
            }

            logger.LogInformation("Finalizado scraping para o domínio: {Domain}", domain);
        }

        logger.LogInformation(
            "Resumo da execução: Total de {PageCount} páginas processadas, {TotalItemCount} itens salvos.", pages,
            totalItems);

        return new ScrapeResult("craigslist", today, pages, totalItems, itemsFile, null);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}
