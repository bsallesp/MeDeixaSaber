using System.Text;
using System.Globalization;

namespace ScrapperApi.CsvProcessor
{
    public static class RemoveDuplicates
    {
        static readonly string[] Header = new[]
        {
            "captured_at_utc","url","title","ref_id","location","when","post_date","phone","state","description"
        };

        static string NowStamp() => DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss");
        static string OutDir() => Path.Combine(AppContext.BaseDirectory, "data", "Saved");

        static List<string> FindInputFiles(string dir) =>
            Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*-items-*.csv", SearchOption.TopDirectoryOnly).OrderBy(x => x).ToList()
                : new List<string>();

        static List<string[]> ParseCsv(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            var rows = new List<string[]>();
            if (lines.Length == 0) return rows;
            var start = 0;
            if (lines[0].StartsWith("captured_at_utc")) start = 1;
            for (int i = start; i < lines.Length; i++)
            {
                var fields = SplitCsvLine(lines[i]);
                if (fields.Length < Header.Length)
                {
                    var fix = new string[Header.Length];
                    Array.Copy(fields, fix, fields.Length);
                    for (int k = fields.Length; k < Header.Length; k++) fix[k] = "";
                    rows.Add(fix);
                }
                else if (fields.Length > Header.Length)
                {
                    var take = new string[Header.Length];
                    Array.Copy(fields, take, Header.Length);
                    rows.Add(take);
                }
                else
                {
                    rows.Add(fields);
                }
            }
            return rows;
        }

        static string[] SplitCsvLine(string line)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        list.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            list.Add(sb.ToString());
            return list.ToArray();
        }

        static string CsvEscape(string? s)
        {
            s ??= "";
            bool need = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (!need) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        static string JoinCsv(string[] fields)
        {
            var buf = new string[Header.Length];
            for (int i = 0; i < Header.Length; i++) buf[i] = CsvEscape(i < fields.Length ? fields[i] ?? "" : "");
            return string.Join(",", buf);
        }

        static string Key(string url, string refId, string postDate)
        {
            var id = string.IsNullOrWhiteSpace(refId) ? url.Trim() : refId.Trim();
            var pd = (postDate ?? "").Trim();
            return $"{id}||{pd}";
        }

        static DateTime ParseCaptured(string s)
        {
            if (DateTime.TryParse(s, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)) return dt;
            return DateTime.MinValue;
        }

        public static async Task<(string outputCsv, string reportTxt)> RunAsync(string? inputDir = null)
        {
            var dir = inputDir ?? OutDir();
            Directory.CreateDirectory(dir);

            var inputs = FindInputFiles(dir);
            var report = new StringBuilder();
            report.AppendLine($"RUN {DateTime.UtcNow:O}");
            report.AppendLine($"INPUT_DIR\t{dir}");
            report.AppendLine($"INPUT_COUNT\t{inputs.Count}");

            var all = new List<(string src, string[] row)>();
            long rawLines = 0;
            foreach (var file in inputs)
            {
                var rows = ParseCsv(file);
                rawLines += rows.Count;
                foreach (var r in rows) all.Add((Path.GetFileName(file), r));
                report.AppendLine($"FILE_READ\t{Path.GetFileName(file)}\tROWS\t{rows.Count}");
            }

            var idx = new Dictionary<string, (string src, string[] row, DateTime captured)>(StringComparer.OrdinalIgnoreCase);
            long dupCount = 0;
            long noKey = 0;

            foreach (var item in all)
            {
                var r = item.row;
                var captured = r[0];
                var url = r[1];
                var refId = r[3];
                var postDate = r[6];

                if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(refId))
                {
                    noKey++;
                    continue;
                }

                var k = Key(url, refId, postDate);
                var cap = ParseCaptured(captured);

                if (!idx.TryGetValue(k, out var cur))
                {
                    idx[k] = (item.src, r, cap);
                }
                else
                {
                    if (cap > cur.captured)
                    {
                        idx[k] = (item.src, r, cap);
                        dupCount++;
                    }
                    else
                    {
                        dupCount++;
                    }
                }
            }

            var kept = idx.Values.Select(v => v.row).ToList();

            var stamp = NowStamp();
            var outCsv = Path.Combine(dir, $"dedup-processed-{stamp}.csv");
            var outTxt = Path.Combine(dir, $"dedup-report-{stamp}.txt");

            using (var sw = new StreamWriter(outCsv, false, new UTF8Encoding(false)))
            {
                await sw.WriteLineAsync(string.Join(",", Header));
                foreach (var r in kept)
                    await sw.WriteLineAsync(JoinCsv(r));
            }

            report.AppendLine($"RAW_ROWS\t{rawLines}");
            report.AppendLine($"KEPT_ROWS\t{kept.Count}");
            report.AppendLine($"REMOVED_DUPLICATES\t{dupCount}");
            report.AppendLine($"SKIPPED_NO_KEY\t{noKey}");

            var byPostDate = kept.GroupBy(r => r[6] ?? "").OrderByDescending(g => g.Count()).Take(10);
            foreach (var g in byPostDate)
                report.AppendLine($"TOP_POST_DATE\t{g.Key}\t{g.Count()}");

            var byDomain = kept
                .GroupBy(r => DomainOf(r[1] ?? ""))
                .OrderByDescending(g => g.Count());
            foreach (var g in byDomain)
                report.AppendLine($"DOMAIN_COUNT\t{g.Key}\t{g.Count()}");

            await File.WriteAllTextAsync(outTxt, report.ToString(), Encoding.UTF8);

            return (outCsv, outTxt);
        }

        static string DomainOf(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var u)) return u.Host.ToLowerInvariant();
            return "";
        }
    }
}
