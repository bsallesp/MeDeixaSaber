using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MDS.Api.Tests.Middleware;

public sealed class WebAppFactoryRateLimitedFast : WebApplicationFactory<Program>
{
    public WebAppFactoryRateLimitedFast()
    {
        Environment.SetEnvironmentVariable("RateLimit__PermitLimit", "2");
        Environment.SetEnvironmentVariable("RateLimit__WindowSeconds", "2");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "iss");
        Environment.SetEnvironmentVariable("Jwt__Audience", "aud");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", new string('k', 32));
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:SigningKey"] = new string('k', 32),
                ["Jwt:ExpirationMinutes"] = "60",
                ["RateLimit:PermitLimit"] = "2",
                ["RateLimit:WindowSeconds"] = "2"
            };
            cfg.AddInMemoryCollection(dict);
        });
    }
}

public class RateLimiter_WindowReset_Tests(WebAppFactoryRateLimitedFast f) : IClassFixture<WebAppFactoryRateLimitedFast>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task UnderLimit_Then429_ThenReset_AllowsAgain()
    {
        var r1 = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
        var r2 = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var r3 = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);
        await Task.Delay(2500);
        var r4 = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
        Assert.Equal(HttpStatusCode.OK, r4.StatusCode);
    }
}
