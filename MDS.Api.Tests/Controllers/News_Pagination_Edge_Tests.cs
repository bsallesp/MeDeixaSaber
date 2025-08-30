using System.Net;
using System.Net.Http.Json;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Api.Tests.Controllers;

public sealed class News_Pagination_Edge_Tests : IClassFixture<WebApplicationFactory<Program>>
{
    sealed class SuccessGetTopNewsHandler : IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>
    {
        public Task<IReadOnlyList<OutsideNews>> Handle(GetTopNewsQuery request, CancellationToken ct = default)
        {
            var n = request.PageSize <= 0 ? 0 : request.PageSize;
            var now = DateTime.UtcNow;
            var list = Enumerable.Range(1, n).Select(i => new OutsideNews
            {
                Title = $"News {i}",
                Content = $"Content {i}",
                Source = "TestSource",
                Url = $"https://example.com/{i}",
                PublishedAt = now.AddMinutes(-i),
                CreatedAt = now
            }).ToList();
            return Task.FromResult<IReadOnlyList<OutsideNews>>(list);
        }
    }

    readonly HttpClient _client;

    public News_Pagination_Edge_Tests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>, SuccessGetTopNewsHandler>();
            });
        }).CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Tests/1.0");
    }

    [Fact]
    public async Task GetTop_Default_PageSize_Returns_10()
    {
        var resp = await _client.GetAsync("/api/news/top");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<OutsideNews>>();
        Assert.NotNull(items);
        Assert.Equal(10, items!.Count);
    }

    [Fact]
    public async Task GetTop_PageSize_Zero_Returns_Empty()
    {
        var resp = await _client.GetAsync("/api/news/top?pageSize=0");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<OutsideNews>>();
        Assert.NotNull(items);
        Assert.Empty(items!);
    }

    [Fact]
    public async Task GetTop_PageSize_Negative_Returns_Empty()
    {
        var resp = await _client.GetAsync("/api/news/top?pageSize=-5");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<OutsideNews>>();
        Assert.NotNull(items);
        Assert.Empty(items!);
    }
}
