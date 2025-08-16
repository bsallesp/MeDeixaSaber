using System.Net;
using Scraper.Runner.Services;

namespace Scraper.Runner;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        using var http = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });
        http.Timeout = TimeSpan.FromSeconds(25);
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 Chrome/124");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");

        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var todayIso = today.ToString("yyyy-MM-dd");
        var yesterdayIso = yesterday.ToString("yyyy-MM-dd");

        string GetArg(string name)
        {
            var v = args.FirstOrDefault(a => a.StartsWith($"--{name}=", StringComparison.OrdinalIgnoreCase));
            return v == null ? "" : v[(name.Length + 3)..];
        }

        var fromArg = GetArg("from");
        var toArg   = GetArg("to");

        var fromIso = string.IsNullOrWhiteSpace(fromArg) ? Environment.GetEnvironmentVariable("RANGE_FROM") : fromArg;
        var toIso   = string.IsNullOrWhiteSpace(toArg)   ? Environment.GetEnvironmentVariable("RANGE_TO")   : toArg;

        if (string.IsNullOrWhiteSpace(fromIso)) fromIso = yesterdayIso;
        if (string.IsNullOrWhiteSpace(toIso))   toIso   = todayIso;

        if (string.CompareOrdinal(fromIso, toIso) > 0) (fromIso, toIso) = (toIso, fromIso);

        var runAchei   = args.Length == 0 || args.Any(a => a.Equals("acheiusa", StringComparison.OrdinalIgnoreCase) || a.Equals("achei", StringComparison.OrdinalIgnoreCase) || a.Equals("au", StringComparison.OrdinalIgnoreCase));
        var runOpAjuda = args.Length == 0 || args.Any(a => a.Equals("opajuda", StringComparison.OrdinalIgnoreCase) || a.Equals("oa", StringComparison.OrdinalIgnoreCase));

        if (!runAchei && !runOpAjuda)
        {
            Console.WriteLine("Uso: dotnet run -- [acheiusa|opajuda] [--from=YYYY-MM-DD] [--to=YYYY-MM-DD]");
            Environment.ExitCode = 2;
            return;
        }

        var container = Environment.GetEnvironmentVariable("BLOB_CONTAINER") ?? "scraped";
        var useLocal = args.Any(a => a.Equals("local", StringComparison.OrdinalIgnoreCase)) 
                       || Environment.GetEnvironmentVariable("SAVE_LOCAL") == "1";

        IUploader uploader = useLocal
            ? new LocalFileUploader("test-output")
            : new AzureBlobUploaderAdapter(container);

        var tasks = new List<Task>();

        if (runAchei)
        {
            tasks.Add(Run("AcheiUSA", async () =>
            {
                var res = await Sites.AcheiUSA.ShortScraper.RunAsync(http, fromIso, toIso, todayIso);
                var sr  = PostProcessor.Materialize(res);
                await PostProcessor.FilterAndUploadAsync(sr, uploader, todayIso);
                return res;
            }));
        }

        if (runOpAjuda)
        {
            tasks.Add(Run("OpAjuda", async () =>
            {
                var res = await Sites.OpAjuda.ShortScraper.RunAsync(http, fromIso, toIso, todayIso);
                var sr  = PostProcessor.Materialize(res);
                await PostProcessor.FilterAndUploadAsync(sr, uploader, todayIso);
                return res;
            }));
        }

        await Task.WhenAll(tasks);
    }

    private static async Task Run(string label, Func<Task<object>> action)
    {
        try
        {
            Console.WriteLine($"[{label}] START {DateTime.UtcNow:O}");
            var result = await action();
            Console.WriteLine($"[{label}] OK -> {System.Text.Json.JsonSerializer.Serialize(result)}");
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"[{label}] FAIL: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
}
