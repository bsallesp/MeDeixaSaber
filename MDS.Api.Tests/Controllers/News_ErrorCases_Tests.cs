using System.Net;
using MDS.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;

namespace MDS.Api.Tests.Controllers;

public sealed class WebAppFactoryNewsErrors : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>, ThrowingGetTopNewsHandler>();
        });
    }
}

public class News_ErrorCases_Tests : IClassFixture<WebApplicationFactory<Program>>, IClassFixture<WebAppFactoryNewsErrors>
{
    readonly HttpClient _defaultClient;
    readonly HttpClient _throwClient;

    public News_ErrorCases_Tests(WebApplicationFactory<Program> def, WebAppFactoryNewsErrors thr)
    {
        _defaultClient = def.WithWebHostBuilder(b => b.UseEnvironment("Testing")).CreateClient();
        _throwClient = thr.CreateClient();
    }

    [Fact]
    public async Task GetTop_PageSize_NonInteger_Should400()
    {
        var resp = await _defaultClient.GetAsync("/api/news/top?pageSize=abc");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task GetTop_HandlerThrows_Should500()
    {
        var resp = await _throwClient.GetAsync("/api/news/top?pageSize=3");
        Assert.Equal(HttpStatusCode.InternalServerError, resp.StatusCode);
    }
}