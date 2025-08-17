namespace MDS.Scraper.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MeDeixaSaber.Core.Models;
using Data.Repositories;

public sealed class DedupAndPersist(ClassifiedsRepository repo)
{
    static string Key(Classified c)
    {
        var k = c.RefId?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(k)) return $"ref:{k}";
        if (!string.IsNullOrWhiteSpace(c.Url)) return $"url:{c.Url.Trim().ToLowerInvariant()}";
        var t = (c.Title ?? "").Trim().ToLowerInvariant();
        var p = (c.Phone ?? "").Trim().ToLowerInvariant();
        return $"tp:{t}|{p}";
    }

    public async Task<int> UpsertNewAsync(IEnumerable<Classified> scraped, DateTime dayUtc, CancellationToken ct=default)
    {
        var existing = await repo.GetByDayAsync(dayUtc);
        var keys = new HashSet<string>(existing.Select(Key));
        var toInsert = scraped.Where(s => !keys.Contains(Key(s))).ToList();
        foreach (var c in toInsert) await repo.InsertAsync(c);
        return toInsert.Count;
    }
}