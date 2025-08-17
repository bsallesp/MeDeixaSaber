using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MDS.Scraper.Scrapers.AcheiUsa
{
    public static class Scraper
    {
        private static string Clean(string? s) =>
            Regex.Replace(HtmlEntity.DeEntitize(s ?? ""), @"\s+", " ").Trim();

        private static string NowIso() => DateTime.UtcNow.ToString("O");

        private static async Task AppendToFile(string file, string line, SemaphoreSlim gate)
        {
            var dir = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await gate.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(file, line + Environment.NewLine, Encoding.UTF8);
                var len = new FileInfo(file).Length;
                Console.WriteLine($"[AppendToFile] WROTE line to {file} (tamanho: {len} bytes)");
            }
            finally
            {
                gate.Release();
            }
        }

        private static string CsvEscape(string? s)
        {
            s ??= "";
            var needsQuote = s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r');
            var val = s.Replace("\"", "\"\"");
            return needsQuote ? $"\"{val}\"" : val;
        }

        private static readonly string[] NoisePhrases =
        {
            "autenticando", "Compartilhe", "Publicidade", "Registrar", "Login",
            "Categorias", "Classificados", "Termos de uso", "Política de privacidade",
            "Todos direitos reservados", "AcheiUSA", "Newspaper", "Home",
            "Automóveis", "Imóveis", "Empregos", "Mercadorias", "Negócios", "Serviços",
            "Workshops, Aulas e Cursos", "Pessoais", "Mensagens", "Quartos, Vagas e Roommates",
            "Por favor, digite seus dados abaixo",
            "Login para escrever resenha",
            "Responder por email",
            "Adicione seu comentário",
            "Por favor digite seu comentário",
            "Mostrar mais", "Mostrar menos"
        };

        private static string? DerivePostDateFromWhen(string? whenText, DateTime? todayUtc = null)
        {
            if (string.IsNullOrWhiteSpace(whenText)) return null;
            var t = whenText.Trim();
            todayUtc ??= DateTime.UtcNow;
            var mDate = Regex.Match(t, @"(?i)Data\s*:\s*(\d{2}/\d{2}/\d{4})");
            if (mDate.Success)
            {
                if (DateTime.TryParseExact(mDate.Groups[1].Value, "dd/MM/yyyy", new CultureInfo("pt-BR"),
                        DateTimeStyles.AssumeLocal, out var dt))
                    return dt.ToString("yyyy-MM-dd");
                return mDate.Groups[1].Value;
            }

            var mRel = Regex.Match(t,
                @"(?i)\b(\d+)\s+(hora|horas|dia|dias|semana|semanas|m[eê]s|meses|ano|anos)\s+atr[aá]s\b");
            if (mRel.Success)
            {
                var qty = int.Parse(mRel.Groups[1].Value);
                var unit = mRel.Groups[2].Value.ToLowerInvariant();
                var baseDate = todayUtc.Value;
                DateTime dt = baseDate;
                if (unit.StartsWith("hora")) dt = baseDate.AddHours(-qty);
                else if (unit.StartsWith("dia")) dt = baseDate.AddDays(-qty);
                else if (unit.StartsWith("semana")) dt = baseDate.AddDays(-7 * qty);
                else if (unit.StartsWith("m")) dt = baseDate.AddMonths(-qty);
                else if (unit.StartsWith("ano")) dt = baseDate.AddYears(-qty);
                return dt.Date.ToString("yyyy-MM-dd");
            }

            return null;
        }

        private static readonly string[] CutMarkers =
        {
            "Anunciante", "Telefone", "Registrado", "Mais anúncios", "Comentários",
            "Comments", "Related", "Leia mais", "Share"
        };

        private static readonly HashSet<string> CountryNoiseSingles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Brasil", "Portugal", "United States", "Estados Unidos", "United States of America"
        };

        private static bool LooksLikeCountryCityDump(string s)
        {
            var t = Clean(s);
            var commas = t.Count(c => c == ',' || c == '|');
            if (commas >= 10) return true;
            if (t.Contains("Brasil Portugal United States", StringComparison.OrdinalIgnoreCase)) return true;
            if (CountryNoiseSingles.Contains(t)) return true;
            var caps = Regex.Matches(t, @"\b([A-Z][a-zA-ZÀ-ÖØ-öø-ÿ\-']{2,})\b").Count;
            return caps >= 40;
        }

        private static string TruncateAtMarkers(string text)
        {
            foreach (var m in CutMarkers)
            {
                var idx = text.IndexOf(m, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) return text[..idx].Trim();
            }

            return text.Trim();
        }

        private static string NormalizePhone(string input)
        {
            var digits = Regex.Replace(input ?? "", @"\D", "");
            if (digits.Length == 11 && digits.StartsWith("1")) digits = digits[1..];
            if (digits.Length == 10) return $"({digits[..3]}) {digits.Substring(3, 3)}-{digits.Substring(6, 4)}";
            return string.IsNullOrWhiteSpace(input) ? "" : Clean(input);
        }

        private static string ExtractTitle(HtmlDocument doc)
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
            var t = Clean(h?.InnerText ?? "");
            if (string.Equals(t, "Localização", StringComparison.OrdinalIgnoreCase)) t = "";
            return t;
        }

        private static HtmlNode FindMain(HtmlDocument doc)
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

        private static void DropGarbageNodes(HtmlNode root)
        {
            var xp = ".//*[self::nav or self::footer or self::form or self::aside or " +
                     "contains(@class,'footer') or contains(@class,'sidebar') or contains(@class,'related') or " +
                     "contains(@class,'similar') or contains(@class,'share') or contains(@class,'social') or " +
                     "contains(@class,'comment') or contains(@class,'breadcrumbs') or contains(@class,'menu') or contains(@class,'header') or " +
                     "contains(@class,'reviews-widget') or contains(@id,'emailToSeller')]";
            var nodes = root.SelectNodes(xp);
            if (nodes != null)
                foreach (var n in nodes)
                    n.Remove();
        }

        private static IEnumerable<string> ExtractPhones(HtmlNode scope)
        {
            var telLinks = scope.SelectNodes(".//a[starts-with(@href,'tel:')]")
                ?.Select(a => a.GetAttributeValue("href", "").Replace("tel:", "")) ?? Enumerable.Empty<string>();
            var inline = Regex.Matches(scope.InnerText, @"(\+1[\s\-\.]?)?\(?\d{3}\)?[\s\-\.]?\d{3}[\s\-\.]?\d{4}");
            return telLinks.Concat(inline.Select(m => m.Value))
                .Select(NormalizePhone)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct();
        }

        private static string BuildDescriptionFromNodes(IEnumerable<HtmlNode> nodes)
        {
            var blocks = nodes
                .Select(n => Clean(n.InnerText))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Where(s =>
                    !LooksLikeCountryCityDump(s) &&
                    !CountryNoiseSingles.Contains(s) &&
                    !NoisePhrases.Any(nz => s.IndexOf(nz, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();
            var desc = string.Join("\n", blocks);
            desc = TruncateAtMarkers(desc);
            if (Regex.IsMatch(desc, @"^Por favor[, ]+digite seus dados abaixo$", RegexOptions.IgnoreCase)) desc = "";
            if (desc.Length > 4000) desc = desc[..4000];
            return desc;
        }

        private static string? ParsePostDateFromText(string text)
        {
            var m = Regex.Match(text, @"\(\s*Data:\s*(\d{2}/\d{2}/\d{4})\s*\)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                if (DateTime.TryParseExact(m.Groups[1].Value, "dd/MM/yyyy", new CultureInfo("pt-BR"),
                        DateTimeStyles.AssumeLocal, out var dt))
                    return dt.ToString("yyyy-MM-dd");
                return m.Groups[1].Value;
            }

            return null;
        }

        private static (string title, string description, string refId, string? phone, string? location, string? when,
            string? postDate)
            ParseItem(string url, string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var title = ExtractTitle(doc);
            string refId = "";
            var mUrl = Regex.Match(url, @"/ad/(\d+)/", RegexOptions.IgnoreCase);
            if (mUrl.Success) refId = mUrl.Groups[1].Value;
            if (string.IsNullOrEmpty(refId))
            {
                var mRef = Regex.Match(doc.DocumentNode.InnerText, @"Ref:\s*(\d+)", RegexOptions.IgnoreCase);
                if (mRef.Success) refId = mRef.Groups[1].Value;
            }

            string? postDate = null;
            var main = FindMain(doc);
            DropGarbageNodes(main);
            var candidates = new List<string>();
            var descUserHtml = main.SelectSingleNode(".//div[contains(@class,'user-html')]")
                               ?? main.SelectSingleNode(
                                   ".//*[contains(@class,'ads-details')]//div[contains(@class,'user-html')]");
            if (descUserHtml != null) candidates.Add(BuildDescriptionFromNodes(new[] { descUserHtml }));
            var adsNodes =
                main.SelectNodes(".//*[contains(@class,'ads-details')]//p | .//*[contains(@class,'ads-details')]//li");
            if (adsNodes != null) candidates.Add(BuildDescriptionFromNodes(adsNodes));
            var panel = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'panel-body')]");
            if (panel != null)
            {
                postDate ??= ParsePostDateFromText(panel.InnerText);
                var ps = panel.SelectNodes(".//p") ?? new HtmlNodeCollection(null);
                var usable = ps.Where(p =>
                {
                    var txt = Clean(p.InnerText);
                    return !string.IsNullOrEmpty(txt)
                           && !Regex.IsMatch(txt, @"^(Nome|Telefone|Ref)\s*:", RegexOptions.IgnoreCase)
                           && !Regex.IsMatch(txt, @"^\(Data:", RegexOptions.IgnoreCase);
                });
                candidates.Add(BuildDescriptionFromNodes(usable));
            }

            if (!candidates.Any() || string.IsNullOrWhiteSpace(candidates.MaxBy(s => s?.Length ?? 0)))
            {
                var anyUserHtml = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'user-html')]");
                if (anyUserHtml != null) candidates.Add(BuildDescriptionFromNodes(new[] { anyUserHtml }));
            }

            if (!candidates.Any() || string.IsNullOrWhiteSpace(candidates.MaxBy(s => s?.Length ?? 0)))
            {
                var allP = doc.DocumentNode.SelectNodes("//p") ?? new HtmlNodeCollection(null);
                candidates.Add(BuildDescriptionFromNodes(allP));
            }

            var description = candidates.Where(s => !string.IsNullOrWhiteSpace(s)).OrderByDescending(s => s.Length)
                .FirstOrDefault() ?? "";
            string? location = null;
            var locNode = main.SelectSingleNode(".//*[contains(@class,'fa-map-marker')]/following-sibling::a")
                          ?? main.SelectSingleNode(".//i[contains(@class,'fa-map-marker')]/following-sibling::a");
            if (locNode != null) location = Clean(locNode.InnerText);
            if (string.IsNullOrWhiteSpace(location))
            {
                var locMatch = Regex.Match(doc.DocumentNode.InnerText, @"([A-Za-zÀ-ÖØ-öø-ÿ\.\-\' ]+),\s*United States");
                if (locMatch.Success) location = Clean(locMatch.Value);
            }

            string? when = null;
            var whenNode =
                main.SelectSingleNode(
                    ".//*[contains(@class,'fa-clock') or contains(@class,'fa-clock-o')]/following-sibling::a");
            if (whenNode != null) when = Clean(whenNode.InnerText);
            if (string.IsNullOrWhiteSpace(when))
            {
                var whenMatch = Regex.Match(main.InnerText,
                    @"\b\d+\s+(?:dia|dias|hora|horas|semana|semanas|m[eê]s|meses|ano|anos)\s+atr[aá]s\b",
                    RegexOptions.IgnoreCase);
                if (whenMatch.Success) when = Clean(whenMatch.Value);
            }

            if (string.IsNullOrWhiteSpace(postDate))
            {
                var postDateNode = doc.DocumentNode.SelectSingleNode("//p[contains(@class,'description-date')]");
                if (postDateNode != null) postDate = ParsePostDateFromText(postDateNode.InnerText);
                if (string.IsNullOrWhiteSpace(postDate)) postDate = ParsePostDateFromText(doc.DocumentNode.InnerText);
            }

            var phone = ExtractPhones(main).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(phone))
            {
                var advertiser = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'aside-body')]");
                var pTel = advertiser?.SelectSingleNode(
                               ".//p[strong[contains(translate(normalize-space(.),'TELFONE','telefone'),'telefone')]]")
                           ?? advertiser?.SelectSingleNode(".//p[contains(.,'Telefone') or contains(.,'Phone')]");
                if (pTel != null)
                {
                    var raw = Regex.Replace(pTel.InnerText, @"(?i)Telefone\s*:?", "", RegexOptions.IgnoreCase);
                    var cand = ExtractPhones(pTel).FirstOrDefault() ?? raw;
                    phone = NormalizePhone(cand);
                }
            }

            if (string.IsNullOrWhiteSpace(phone)) phone = ExtractPhones(doc.DocumentNode).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Clean(description.Split('\n').FirstOrDefault() ?? "");
                if (title.Length > 140) title = title[..140];
            }

            return (title, description, refId, phone, location, when, postDate);
        }

        private static List<(string url, string? location, string? when)> ExtractLinks(string html, Uri baseUri)
        {
            var results = new List<(string url, string? location, string? when)>();
            if (string.IsNullOrWhiteSpace(html)) return results;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var cardNodes = doc.DocumentNode.SelectNodes(
                "//*[contains(@class,'listing') or contains(@class,'items') or contains(@class,'results') or " +
                "contains(@class,'loop') or contains(@class,'ad-list') or contains(@class,'archive') or " +
                "contains(@class,'row') or contains(@class,'card') or self::ul or self::ol]"
            ) ?? new HtmlNodeCollection(null);
            if (cardNodes.Count == 0) cardNodes = new HtmlNodeCollection(null) { doc.DocumentNode };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var scope in cardNodes)
            {
                var items = scope.SelectNodes(".//*[self::article or self::li or self::div]") ??
                            new HtmlNodeCollection(null);
                if (items.Count == 0) items = new HtmlNodeCollection(null) { scope };
                foreach (var item in items)
                {
                    var a = item.SelectSingleNode(".//a[contains(@href,'/ad/')]");
                    if (a == null) continue;
                    var href = a.GetAttributeValue("href", "")?.Trim();
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    if (href.StartsWith("#") ||
                        href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) continue;
                    string abs;
                    if (Uri.TryCreate(baseUri, href, out var absUri)) abs = absUri.ToString();
                    else abs = href;
                    if (!seen.Add(abs)) continue;

                    string? when = null;
                    var whenNode =
                        item.SelectSingleNode(
                            ".//*[contains(@class,'fa-clock') or contains(@class,'fa-clock-o')]/following-sibling::a") ??
                        item.SelectSingleNode(
                            ".//*[contains(@class,'fa-clock') or contains(@class,'fa-clock-o')]/following-sibling::*[1]") ??
                        item.SelectSingleNode(
                            ".//*[contains(@class,'date') or contains(@class,'time') or contains(@class,'posted') or contains(@class,'ago')]") ??
                        item.SelectSingleNode(
                            ".//li[contains(@class,'date') or contains(@class,'time') or contains(@class,'posted') or contains(@class,'ago')]");
                    if (whenNode != null) when = Clean(whenNode.InnerText);
                    if (string.IsNullOrWhiteSpace(when))
                    {
                        var nearText = Clean(item.InnerText);
                        var mWhen = Regex.Match(nearText,
                            @"\b\d+\s+(?:hora|horas|dia|dias|semana|semanas|m[eê]s|meses|ano|anos)\s+atr[aá]s\b",
                            RegexOptions.IgnoreCase);
                        if (mWhen.Success) when = Clean(mWhen.Value);
                    }

                    string? location = null;
                    var locNode =
                        item.SelectSingleNode(".//*[contains(@class,'fa-map-marker')]/following-sibling::a") ??
                        item.SelectSingleNode(".//*[contains(@class,'fa-map-marker')]/following-sibling::*[1]") ??
                        item.SelectSingleNode(
                            ".//*[contains(@class,'location') or contains(@class,'local') or contains(@class,'city') or contains(@class,'place')]");
                    if (locNode != null) location = Clean(locNode.InnerText);
                    if (string.IsNullOrWhiteSpace(location))
                    {
                        var nearText = Clean(item.InnerText);
                        var mLoc = Regex.Match(nearText, @"([A-Za-zÀ-ÖØ-öø-ÿ\.\-\' ]+),\s*United States");
                        if (mLoc.Success) location = Clean(mLoc.Value);
                    }

                    results.Add((abs, location, when));
                }
            }

            return results
                .GroupBy(x => x.url, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        public static async Task<object> RunAsync(HttpClient http, string today)
        {
            var categories = new[]
            {
                "https://classificados.acheiusa.com/category/10/automoveis/",
                "https://classificados.acheiusa.com/category/11/imoveis/",
                "https://classificados.acheiusa.com/category/12/emprego/",
                "https://classificados.acheiusa.com/category/13/mercadorias/",
                "https://classificados.acheiusa.com/category/14/negocios/",
                "https://classificados.acheiusa.com/category/15/servicos/",
                "https://classificados.acheiusa.com/category/19/aulas-e-cursos/",
                "https://classificados.acheiusa.com/category/17/pessoas/"
            };

            // Escreve em /tmp para evitar 'Permission denied' no container
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
            var itemsCsv = Path.Combine("/tmp", $"acheiusa-items-{stamp}.csv");
            var logFile = Path.Combine("/tmp", $"acheiusa-log-{stamp}.txt");

            var logLock = new SemaphoreSlim(1, 1);
            var dataLock = new SemaphoreSlim(1, 1);

            await AppendToFile(itemsCsv,
                string.Join(",", "captured_at_utc", "url", "title", "ref_id", "location", "when", "post_date", "phone",
                    "state", "description"),
                dataLock);

            var totalItems = 0;
            var pages = 0;

            var seenRefIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string RefFromUrl(string u)
            {
                var m = Regex.Match(u ?? "", @"/ad/(\d+)/", RegexOptions.IgnoreCase);
                return m.Success ? m.Groups[1].Value : "";
            }

            foreach (var catBase in categories)
            {
                int emptyStreak = 0; // páginas seguidas sem itens do dia

                for (int page = 1; page <= 500; page++)
                {
                    var url = page == 1 ? catBase : (catBase.TrimEnd('/') + "/page/" + page + "/");
                    string html;
                    try
                    {
                        await AppendToFile(logFile, $"{NowIso()}\tPAGE_START\t{url}", logLock);
                        html = await http.GetStringAsync(url);
                    }
                    catch (Exception ex)
                    {
                        await AppendToFile(logFile, $"{NowIso()}\tPAGE_FAIL\t{url}\t{ex.Message}", logLock);
                        break;
                    }

                    var links = ExtractLinks(html, new Uri(url)); // List<(string url, string? location, string? when)>
                    if (links.Count == 0)
                    {
                        await AppendToFile(logFile, $"{NowIso()}\tPAGE_EMPTY\t{url}", logLock);
                        emptyStreak++;
                        if (emptyStreak >= 2) break;
                        continue;
                    }

                    await AppendToFile(logFile, $"{NowIso()}\tPAGE_LINKS\t{url}\t{links.Count}", logLock);

                    var sem = new SemaphoreSlim(6);
                    var pageNewToday = 0; // quantos itens do dia encontramos nesta página

                    await Task.WhenAll(links.Select(async tpl =>
                    {
                        var link = tpl.url;

                        var refIdFromLink = RefFromUrl(link);
                        if (!string.IsNullOrWhiteSpace(refIdFromLink) && !seenRefIds.Add(refIdFromLink)) return;
                        if (string.IsNullOrWhiteSpace(refIdFromLink) && !seenUrls.Add(link)) return;

                        await sem.WaitAsync();
                        try
                        {
                            string itemHtml;
                            try
                            {
                                itemHtml = await http.GetStringAsync(link);
                            }
                            catch (Exception ex)
                            {
                                await AppendToFile(logFile, $"{NowIso()}\tITEM_FETCH_FAIL\t{link}\t{ex.Message}",
                                    logLock);
                                return;
                            }

                            // ParseItem do AcheiUSA retorna 7 campos
                            var (title, description, refId, phone, itemLocation, itemWhen, itemPostDate) =
                                ParseItem(link, itemHtml);

                            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
                            {
                                await AppendToFile(logFile, $"{NowIso()}\tITEM_PARSE_EMPTY\t{link}", logLock);
                                return;
                            }

                            if (!string.IsNullOrWhiteSpace(refId))
                                seenRefIds.Add(refId);
                            else
                                seenUrls.Add(link);

                            var finalLocation = !string.IsNullOrWhiteSpace(itemLocation) ? itemLocation : tpl.location;
                            var finalWhen = !string.IsNullOrWhiteSpace(itemWhen) ? itemWhen : tpl.when;
                            var finalPostDate = !string.IsNullOrWhiteSpace(itemPostDate)
                                ? itemPostDate
                                : DerivePostDateFromWhen(finalWhen);

                            // mantém somente do dia atual
                            if (finalPostDate != today) return;

                            var line = string.Join(",",
                                CsvEscape(NowIso()),
                                CsvEscape(link),
                                CsvEscape(title),
                                CsvEscape(refId),
                                CsvEscape(finalLocation),
                                CsvEscape(finalWhen),
                                CsvEscape(finalPostDate),
                                CsvEscape(phone),
                                CsvEscape(""),
                                CsvEscape(description));

                            await AppendToFile(itemsCsv, line, dataLock);

                            Interlocked.Increment(ref totalItems);
                            Interlocked.Increment(ref pageNewToday);
                        }
                        finally
                        {
                            sem.Release();
                        }
                    }));

                    pages++;
                    await AppendToFile(logFile, $"{NowIso()}\tPAGE_DONE\t{url}\t{pageNewToday}", logLock);

                    // Se duas páginas seguidas não deram nenhum item do dia, paramos de varrer
                    if (pageNewToday == 0)
                    {
                        emptyStreak++;
                        if (emptyStreak >= 2) break;
                    }
                    else
                    {
                        emptyStreak = 0;
                    }
                }
            }

            await AppendToFile(logFile, $"{NowIso()}\tRUN_SUMMARY\tpages={pages}\ttotalItems={totalItems}", logLock);
            return new { site = "acheiusa", date = today, pages, totalItems, itemsFile = itemsCsv, logFile };
        }
    }
}