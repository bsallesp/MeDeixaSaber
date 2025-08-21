using System.Net;
using System.Text;
using FluentAssertions;
using MDS.Runner.Scraper.Scrapers.AcheiUsa;

namespace MDS.Tests.Scrapers;

file sealed class MappingHandler(Dictionary<string, string> map) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();

        // permite varrer "/page/N/"
        var body = map.GetValueOrDefault(url, "<html><body><div class='listing'></div></body></html>");

        // se pedirem alguma página que não mapeamos, devolvemos HTML "sem links"
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
        // container que o ExtractLinks reconhece (listing/row/card/ul/ol…)
        var items = string.Join("", adUrls.Select(u => $@"<div class='listing'><a href='{u}'>ad</a></div>"));
        return $"<html><body><div class='listing'>{items}</div></body></html>";
    }

    string ItemHtml(
        string ogTitle,
        string? panelDateDdMmYyyy = null,
        string? location = "Boca Raton, United States",
        string? phone = "(561) 111-2222")
    {
        // Monta um HTML mínimo que o scraper entende:
        // - <meta property='og:title'>
        // - <div class='panel-body'><p class='description-date'>(Data: dd/MM/yyyy)</p></div>  (para postDate)
        // - localização (via “, United States”) e telefone (regex)
        var meta = $@"<meta property='og:title' content='{ogTitle}'>";
        var dateP = panelDateDdMmYyyy is null ? "" : $@"<p class='description-date'>(Data: {panelDateDdMmYyyy})</p>";
        var panel = $@"<div class='panel-body'>{dateP}<p>Telefone: {phone}</p></div>";
        var body = $@"
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
        return body;
    }

    [Fact]
    public async Task RunAsync_Writes_ItemsCsv_With_Only_Today()
    {
        // today string no formato esperado pelo scraper
        var today = "2025-08-21";

        // Base category URL usada pelo scraper
        var cat = "https://classificados.acheiusa.com/category/12/emprego/";

        // Ads
        var ad1 = "https://classificados.acheiusa.com/ad/1001/vaga-teste/";
        var ad2 = "https://classificados.acheiusa.com/ad/1002/vaga-teste-2/";

        // Map de respostas
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Página 1 da categoria com 2 anúncios
            [cat] = CatHtml(ad1, ad2),

            // Página 2 vazia → força early stop
            [$"{cat}page/2/"] = "<html><body><div class='listing'></div></body></html>",

            // Item 1 data = hoje
            [ad1] = ItemHtml("Vaga Teste 1", panelDateDdMmYyyy: "21/08/2025"),
            // Item 2 data ≠ hoje → deve ser filtrado fora
            [ad2] = ItemHtml("Vaga Teste 2", panelDateDdMmYyyy: "20/08/2025"),
        };

        using var http = new HttpClient(new MappingHandler(map));

        // Executa
        var result = await AcheiUsaScraper.RunAsync(http, today);

        // A resposta é um objeto anon. com itemsFile/logFile/totalItems/pages
        dynamic dyn = result;
        string itemsFile = dyn.itemsFile;
        string logFile   = dyn.logFile;
        int totalItems   = dyn.totalItems;
        int pages        = dyn.pages;

        // Asserts principais
        File.Exists(itemsFile).Should().BeTrue();
        File.Exists(logFile).Should().BeTrue();
        totalItems.Should().Be(1);                 // só o ad1 tem "Data: 21/08/2025"
        pages.Should().BeGreaterOrEqualTo(1);

        // CSV: header + 1 linha
        var lines = await File.ReadAllLinesAsync(itemsFile, Encoding.UTF8);
        lines.Should().HaveCount(1 + 1);

        // Conferir colunas essenciais da linha (sem depender de ordem de espaços/aspas)
        var dataLine = lines[1];
        dataLine.Should().Contain(ad1);
        dataLine.Should().Contain("Vaga Teste 1");

        // post_date (coluna 7) deve ser today
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

        dynamic dyn = result;
        string itemsFile = dyn.itemsFile;
        int totalItems   = dyn.totalItems;

        totalItems.Should().Be(2);

        var lines = await File.ReadAllLinesAsync(itemsFile, Encoding.UTF8);
        lines.Should().HaveCount(1 + 2);
        lines[1].Should().Contain("Ok 1").And.Contain(today);
        lines[2].Should().Contain("Ok 2").And.Contain(today);
    }
}
