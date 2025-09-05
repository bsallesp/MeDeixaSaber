using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class NewsRelated_EndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    readonly WebApplicationFactory<Program> _factory;

    public NewsRelated_EndpointTests(WebApplicationFactory<Program> f)
        => _factory = f.WithWebHostBuilder(b => b.UseEnvironment("Testing"));

    [Fact]
    public async Task GET_Latest_Should_Return_200_And_Array()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("tests");
        client.DefaultRequestHeaders.Add("X-PoW", "v1:0:0000000000000000");

        var resp = await client.GetAsync("/api/news/related/latest?daysBack=30&topN=5&useContent=0");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var body = await resp.Content.ReadFromJsonAsync<object[]>();
        Assert.NotNull(body);
    }
}