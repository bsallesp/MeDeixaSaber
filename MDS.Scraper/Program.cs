using MDS.Scraper.Services;

namespace MDS.Scraper;

internal abstract class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Starting scrapers...VB00001");
        
        using var http = new HttpClient();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var acheiUsaResult = await Scrapers.AcheiUsa.Scraper.RunAsync(http, today);
        Console.WriteLine($"AcheiUSA: {System.Text.Json.JsonSerializer.Serialize(acheiUsaResult)}");

        var opAjudaResult = await Scrapers.OpAjuda.Scraper.RunAsync(http, today);
        Console.WriteLine($"OpAjuda: {System.Text.Json.JsonSerializer.Serialize(opAjudaResult)}");

        IStorageUploader uploader = args.Contains("--local")
            ? new LocalUploader()
            : new BlobUploader("mdsprodstg04512", "scraped");

        await uploader.SaveAsync("acheiusa", ((dynamic)acheiUsaResult).itemsFile);
        await uploader.SaveAsync("opajuda", ((dynamic)opAjudaResult).itemsFile);
    }
}