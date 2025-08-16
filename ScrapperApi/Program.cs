using System.Net;
using Microsoft.AspNetCore.Mvc;
using ScrapperApi.CsvProcessor;

var builder = WebApplication.CreateBuilder(args);

// HttpClient com gzip e headers básicos
builder.Services.AddHttpClient("scraper")
    .ConfigureHttpClient(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(25);
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 Chrome/124");
        c.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    });

var app = builder.Build();
app.UseHttpsRedirection();

// ÚNICO endpoint: roda o site OpAjuda
app.MapGet("/oascraper", async (IHttpClientFactory httpFactory) =>
{
    var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var res = await ScrapperApi.Sites.OpAjuda.Scraper.RunAsync(httpFactory.CreateClient("scraper"), today);
    return Results.Json(res);
});

// ➕ AcheiUSA: novo endpoint
app.MapGet("/auscraper", async (IHttpClientFactory httpFactory) =>
{
    var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var res = await ScrapperApi.Sites.AcheiUSA.Scraper.RunAsync(httpFactory.CreateClient("scraper"), today);
    return Results.Json(res);
});

app.MapGet("/dedup", async () =>
{
    var fixedDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "MeDeixaSaber", "ScrapperApi", "data"
    );

    var (csv, report) = await RemoveDuplicates.RunAsync(fixedDir);
    return Results.Json(new { outputCsv = csv, reportTxt = report });
});

app.Run();