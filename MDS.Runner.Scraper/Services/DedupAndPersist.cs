using MDS.Data.Repositories;
using MDS.Data.Repositories.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Services;

public sealed class DedupAndPersist(IClassifiedsRepository repo)
{
    public static string Key(Classified c)
    {
        string Normalize(string? s) =>
            (s ?? string.Empty).Trim().ToLowerInvariant()
            .Replace("\r", "").Replace("\n", "").Replace("  ", " ");

        var title = Normalize(c.Title);
        var desc  = Normalize(c.Description);
        var date  = c.CapturedAtUtc;
        return $"t:{title}|d:{desc}|dt:{date}";
    }

    public async Task<int> UpsertNewAsync(IEnumerable<Classified> scraped, DateTime dayUtc,
        CancellationToken ct = default)
    {
        var existing = await repo.GetByDayAsync(dayUtc);
        var keys = new HashSet<string>(existing.Select(Key));
        var toInsert = scraped.Where(s => !keys.Contains(Key(s))).ToList();

        foreach (var c in toInsert) await repo.InsertAsync(c);
        return toInsert.Count;
    }
}