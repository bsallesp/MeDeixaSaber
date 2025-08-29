using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MDS.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MDS.Application.Abstractions.Data;

namespace MDS.Api.Tests.Controllers;

public sealed class WebAppFactoryClassifiedsByDay : WebApplicationFactory<Program>
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

public class Classifieds_ByDay_Tests(WebAppFactoryClassifiedsByDay f) : IClassFixture<WebAppFactoryClassifiedsByDay>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task ByDay_WithResults_ShouldReturnItems()
    {
        var day = DateTime.UtcNow.Date;
        var resp = await _client.GetAsync($"/api/classifieds/by-day?day={day:O}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        items.Should().NotBeNull();
        items!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ByDay_NoResults_ShouldReturnEmpty()
    {
        var resp = await _client.GetAsync("/api/classifieds/by-day?day=1900-01-01");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await resp.Content.ReadFromJsonAsync<List<dynamic>>();
        items.Should().NotBeNull();
        items!.Should().BeEmpty();
    }

    [Fact]
    public async Task ByDay_InvalidDate_Should400()
    {
        var resp = await _client.GetAsync("/api/classifieds/by-day?day=abc");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}