using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MDS.Application.Security;

namespace MDS.Api.Tests.Middleware;

public sealed class WebAppFactoryRateLimitedDisabled : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:SigningKey"] = new string('k', 32),
                ["Jwt:ExpirationMinutes"] = "60"
            };
            cfg.AddInMemoryCollection(dict);
        });
        builder.ConfigureServices(services =>
        {
            var opts = new JwtOptions
            {
                Issuer = "iss",
                Audience = "aud",
                SigningKey = new string('k', 32),
                ExpirationMinutes = 60
            };
            services.AddSingleton(Options.Create(opts));
            services.AddSingleton(opts);
        });
    }
}

public class RateLimiter_Disabled_Tests(WebAppFactoryRateLimitedDisabled f) : IClassFixture<WebAppFactoryRateLimitedDisabled>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task TestingEnv_ShouldNotApplyRateLimiter()
    {
        for (int i = 0; i < 100; i++)
        {
            var resp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }
    }
}