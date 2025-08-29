using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace MDS.Api.Tests.Controllers;

public sealed class NewsControllerTests(WebAppFactoryNews factory) : IClassFixture<WebAppFactoryNews>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetTop_WithPageSize_Returns_ThatManyItems()
    {
        var resp = await _client.GetAsync("/api/news/top?pageSize=3");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        items.Should().NotBeNull();
        items!.Count.Should().Be(3);
    }

    [Fact]
    public async Task GetTop_DefaultPageSize_Returns_10_Items()
    {
        var resp = await _client.GetAsync("/api/news/top");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        items.Should().NotBeNull();
        items!.Count.Should().Be(10);
    }
}