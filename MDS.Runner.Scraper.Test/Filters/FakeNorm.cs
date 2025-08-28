using System.Text.RegularExpressions;
using MeDeixaSaber.Core.Services;

namespace MDS.Runner.Scraper.Test.Filters;

public sealed class FakeNorm : ITitleNormalizationService
{
    public string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var s = title.Trim().ToLowerInvariant();
        s = Regex.Replace(s, "\\s+", " ");
        return s;
    }
}