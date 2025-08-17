using MeDeixaSaber.Core.Models;

namespace MDS.Scraper.Services;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;

public static class ScrapedCsvReader
{
    public static IEnumerable<Classified> Load(string path)
    {
        using var r = new StreamReader(path);
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture){HasHeaderRecord=true};
        using var csv = new CsvReader(r, cfg);

        if (!csv.Read()) yield break;
        csv.ReadHeader();

        while (csv.Read())
        {
            var c = new Classified
            {
                CapturedAtUtc = csv.TryGetField("captured_at_utc", out string? a) && DateTime.TryParse(a, out var dt) ? dt : null,
                Url = csv.GetField("url") ?? "",
                Title = csv.GetField("title") ?? "",
                RefId = csv.GetField("ref_id"),
                Location = csv.GetField("location"),
                ListingWhen = csv.GetField("when"),
                PostDate = csv.GetField("post_date"),
                Phone = csv.GetField("phone"),
                State = csv.GetField("state"),
                Description = csv.GetField("description"),
                IsDuplicate = false
            };
            yield return c;
        }
    }
}