using System.Text;

namespace MDS.Runner.Scraper.Utils;

public static class ScraperIO
{
    static readonly SemaphoreSlim GlobalLock = new(1, 1);

    public static string GetOutputDir()
    {
        var dir = Environment.GetEnvironmentVariable("OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(dir)) dir = Path.GetTempPath();
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task AppendToFile(string file, string line, SemaphoreSlim? gate = null, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var g = gate ?? GlobalLock;
        await g.WaitAsync(ct);
        try
        {
            await File.AppendAllTextAsync(file, line + Environment.NewLine, Encoding.UTF8, ct);
        }
        finally
        {
            g.Release();
        }
    }

    public static string CsvEscape(string? s)
    {
        s ??= "";
        var needsQuote = s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r');
        var val = s.Replace("\"", "\"\"");
        return needsQuote ? $"\"{val}\"" : val;
    }

    public static string Csv(params string[] cols) =>
        string.Join(",", cols.Select(CsvEscape));

    public static string NowIso() => DateTime.UtcNow.ToString("O");
}