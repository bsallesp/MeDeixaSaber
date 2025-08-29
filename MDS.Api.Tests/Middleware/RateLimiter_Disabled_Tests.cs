using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MDS.Api.Tests.Middleware;

public sealed class WebAppFactoryRateLimitedDisabled : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
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