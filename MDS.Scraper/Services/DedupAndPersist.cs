using System.Globalization;

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
    public static string Key(Classified c)
    {
        string Normalize(string? s) =>
            (s ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("\r", "")
            .Replace("\n", "")
            .Replace("  ", " ");
    
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

        Console.WriteLine("Registros a inserir após deduplicação:");
        foreach (var item in toInsert)
        {
            Console.WriteLine(
                $"Title: {item.Title}, Description: {item.Description}, CapturedAtUtc: {item.CapturedAtUtc}");
        }

        foreach (var c in toInsert) await repo.InsertAsync(c);
        Console.WriteLine($"Total de registros adicionados ao banco: {toInsert.Count}");
        return toInsert.Count;
    }
}