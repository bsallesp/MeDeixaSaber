using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MDS.Runner.Scraper.Test.Scrapers.AcheiUsa.Support;

public sealed class MappingHandler(System.Collections.Generic.Dictionary<string, string> map) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
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