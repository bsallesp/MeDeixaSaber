using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace MDS.Api.Tests.Controllers;

public sealed class NewsControllerTests : IClassFixture<WebAppFactoryNews>
{
    readonly HttpClient _client;

    public NewsControllerTests(WebAppFactoryNews factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Tests/1.0");
    }

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