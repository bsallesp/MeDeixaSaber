using System.Net;
using System.Text;
using FluentAssertions;
using MDS.Runner.Scraper.Scrapers.AcheiUsa;

namespace MDS.Runner.Scraper.Test.Scrapers;

file sealed class MappingHandler(Dictionary<string, string> map) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        var body = map.GetValueOrDefault(url, "<html><body><div class='listing'></div></body></html>");
        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "text/html")
        };
        return Task.FromResult(resp);
    }
}

public sealed class AcheiUsaScraperTests
{
    string CatHtml(params string[] adUrls)
    {
        var items = string.Join("", adUrls.Select(u => $@"<div class='listing'><a href='{u}'>ad</a></div>"));
        return $"<html><body><div class='listing'>{items}</div></body></html>";
    }

    string ItemHtml(string ogTitle, string? panelDateDdMmYyyy = null, string? location = "Boca Raton, United States",
        string? phone = "(561) 111-2222")
    {
        var meta = $@"<meta property='og:title' content='{ogTitle}'>";
        var dateP = panelDateDdMmYyyy is null ? "" : $@"<p class='description-date'>(Data: {panelDateDdMmYyyy})</p>";
        var panel = $@"<div class='panel-body'>{dateP}<p>Telefone: {phone}</p></div>";
        return $@"
<html>
  <head>{meta}<title>{ogTitle} - Classificados AcheiUSA</title></head>
  <body>
    <article id='content'>
      {panel}
      <p>{location}</p>
      <p>Descrição do anúncio.</p>
      <a href='tel:5611112222'>Call</a>
    </article>
  </body>
</html>";
    }

    [Fact]
    public async Task RunAsync_Writes_ItemsCsv_With_Only_Today()
    {
        var today = "2025-08-21";
        var cat = "https://classificados.acheiusa.com/category/12/emprego/";
        var ad1 = "https://classificados.acheiusa.com/ad/1001/vaga-teste/";
        var ad2 = "https://classificados.acheiusa.com/ad/1002/vaga-teste-2/";

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [cat] = CatHtml(ad1, ad2),
            [$"{cat}page/2/"] = "<html><body><div class='listing'></div></body></html>",
            [ad1] = ItemHtml("Vaga Teste 1", panelDateDdMmYyyy: "21/08/2025"),
            [ad2] = ItemHtml("Vaga Teste 2", panelDateDdMmYyyy: "20/08/2025"),
        };

        using var http = new HttpClient(new MappingHandler(map));
        var result = await AcheiUsaScraper.RunAsync(http, today);

        File.Exists(result.ItemsFile).Should().BeTrue();
        File.Exists(result.LogFile).Should().BeTrue();
        result.TotalItems.Should().Be(1);
        result.Pages.Should().BeGreaterOrEqualTo(1);

        var lines = await File.ReadAllLinesAsync(result.ItemsFile, Encoding.UTF8);
        lines.Should().HaveCount(2);
        var dataLine = lines[1];
        dataLine.Should().Contain(ad1);
        dataLine.Should().Contain("Vaga Teste 1");
        dataLine.Should().Contain(today);
    }

    [Fact]
    public async Task RunAsync_Collects_Multiple_Ads_For_Today()
    {
        var today = "2025-08-21";
        var cat = "https://classificados.acheiusa.com/category/12/emprego/";
        var ad1 = "https://classificados.acheiusa.com/ad/2001/ok-1/";
        var ad2 = "https://classificados.acheiusa.com/ad/2002/ok-2/";

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [cat] = CatHtml(ad1, ad2),
            [$"{cat}page/2/"] = "<html><body><div class='listing'></div></body></html>",
            [ad1] = ItemHtml("Ok 1", panelDateDdMmYyyy: "21/08/2025"),
            [ad2] = ItemHtml("Ok 2", panelDateDdMmYyyy: "21/08/2025"),
        };

        using var http = new HttpClient(new MappingHandler(map));
        var result = await AcheiUsaScraper.RunAsync(http, today);

        result.TotalItems.Should().Be(2);
        var lines = await File.ReadAllLinesAsync(result.ItemsFile, Encoding.UTF8);
        lines.Should().HaveCount(3);
        lines[1].Should().Contain("Ok 1").And.Contain(today);
        lines[2].Should().Contain("Ok 2").And.Contain(today);
    }
}