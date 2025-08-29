using System.Net;
using System.Net.Http.Json;
using MDS.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MDS.Application.Abstractions.Data;

namespace MDS.Api.Tests.Controllers;

public sealed class WebAppFactoryClassifieds : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IClassifiedsUnifiedReadRepository, FakeClassifiedsUnifiedReadRepository>();
        });
    }
}

public class ClassifiedsControllerTests(WebAppFactoryClassifieds f) : IClassFixture<WebAppFactoryClassifieds>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task GetTop_Defaults_Should200_AndReturnItems()
    {
        var resp = await _client.GetAsync("/api/classifieds/top");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var items = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        Assert.NotNull(items);
        Assert.True(items!.Count > 0);
    }

    [Fact]
    public async Task GetTop_WithTakeSkip_ShouldRespectPaging()
    {
        var page1 = await _client.GetFromJsonAsync<List<dynamic>>("/api/classifieds/top?take=3&skip=0");
        var page2 = await _client.GetFromJsonAsync<List<dynamic>>("/api/classifieds/top?take=3&skip=3");
        Assert.NotNull(page1);
        Assert.NotNull(page2);
        Assert.Equal(3, page1!.Count);
        Assert.Equal(3, page2!.Count);
        Assert.NotEqual(page1![0].id.ToString(), page2![0].id.ToString());
    }
}