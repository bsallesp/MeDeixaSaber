using System.Net;
using MDS.Api.Tests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace MDS.Api.Tests.Middleware;

public sealed class WebAppFactoryCors : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.AddCors(options =>
            {
                options.AddPolicy("FrontendOnly", p =>
                    p.WithOrigins("http://localhost:4200")
                     .AllowAnyHeader()
                     .AllowAnyMethod());
            });
        });
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>();
            cfg.AddInMemoryCollection(dict);
        });
    }
}

public class CorsTests(WebAppFactoryCors f) : IClassFixture<WebAppFactoryCors>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task Allows_ConfiguredOrigin()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/news/top");
        req.Headers.TryAddWithoutValidation("Origin", "http://localhost:4200");
        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("http://localhost:4200", resp.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Blocks_UnlistedOrigin()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/news/top");
        req.Headers.TryAddWithoutValidation("Origin", "http://evil.example");
        var resp = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.False(resp.Headers.TryGetValues("Access-Control-Allow-Origin", out _));
    }
}