using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using System;
using System.Collections.Generic;

namespace MDS.Runner.Scraper.Services;

public interface IClassifiedsFilter
{
    IReadOnlyList<Classified> Filter(IEnumerable<Classified> scraped, IEnumerable<Classified> existing);
}

public sealed class ClassifiedsFilter(ITitleNormalizationService norm) : IClassifiedsFilter
{
    public IReadOnlyList<Classified> Filter(IEnumerable<Classified> scraped, IEnumerable<Classified> existing)
    {
        var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in existing)
            existingKeys.Add($"{c.PostDate:yyyy-MM-dd}|{norm.Normalize(c.Title)}");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return (from s in scraped
            let key = $"{s.PostDate:yyyy-MM-dd}|{norm.Normalize(s.Title)}"
            where !existingKeys.Contains(key)
            where seen.Add(key)
            select s).ToList();
    }
}