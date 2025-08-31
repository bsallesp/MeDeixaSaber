using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Api.Tests.Middleware;

public sealed class WebAppFactoryRateLimitedFast : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:SigningKey"] = new string('k', 64),
                ["Jwt:ExpirationMinutes"] = "60"
            };
            cfg.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            services.AddRateLimiter(o =>
            {
                o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                {
                    var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 2,
                            Window = TimeSpan.FromSeconds(2),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });

            services.AddRouting();
        });

        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/_rl", async ctx =>
                {
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    await ctx.Response.WriteAsJsonAsync(new { ok = true });
                });
            });
        });
    }
}

public class RateLimiter_WindowReset_Tests : IClassFixture<WebAppFactoryRateLimitedFast>
{
    readonly HttpClient _client;

    public RateLimiter_WindowReset_Tests(WebAppFactoryRateLimitedFast f)
    {
        _client = f.CreateClient();
        _client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Tests/1.0");
    }

    [Fact]
    public async Task UnderLimit_Then429_ThenReset_AllowsAgain()
    {
        var r1 = await _client.GetAsync("/_rl");
        var r2 = await _client.GetAsync("/_rl");
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);

        var r3 = await _client.GetAsync("/_rl");
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);

        await Task.Delay(2500);

        var r4 = await _client.GetAsync("/_rl");
        Assert.Equal(HttpStatusCode.OK, r4.StatusCode);
    }
}
