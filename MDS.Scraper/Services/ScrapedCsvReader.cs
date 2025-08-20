using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using MeDeixaSaber.Core.Models;

namespace MDS.Scraper.Services;

public static class ScrapedCsvReader
{
    public static IEnumerable<Classified> Load(string path)
    {
        using var r = new StreamReader(path);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
        using var csv = new CsvReader(r, cfg);

        if (!csv.Read()) yield break;
        csv.ReadHeader();

        while (csv.Read())
        {
            var capturedAtUtc =
                csv.TryGetField("captured_at_utc", out string? a) &&
                DateTime.TryParse(a, null, DateTimeStyles.RoundtripKind, out var dt)
                    ? (DateTime?)dt
                    : null;
            var postDate = csv.TryGetField("post_date", out string? b) && DateTime.TryParse(b, out var dtPost)
                ? (DateTime?)dtPost
                : null;

            var c = new Classified
            {
                CapturedAtUtc = capturedAtUtc,
                Url = csv.GetField("url") ?? "",
                Title = csv.GetField("title") ?? "",
                RefId = csv.GetField("ref_id"),
                Location = csv.GetField("location"),
                ListingWhen = csv.GetField("when"),
                PostDate = postDate,
                Phone = csv.GetField("phone"),
                State = csv.GetField("state"),
                Description = csv.GetField("description"),
                IsDuplicate = false
            };
            yield return c;
        }
    }
}