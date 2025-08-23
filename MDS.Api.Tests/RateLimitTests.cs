using System.Net;
using System.Net.Http.Json;

namespace MDS.Api.Tests;

public class RateLimitTests(WebAppFactoryRateLimited factory) : IClassFixture<WebAppFactoryRateLimited>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Exceeding_Global_Limit_Should_Return_429()
    {
        const int attempts = 65; // Program.cs: 60 req/min por IP
        var got429 = false;

        for (var i = 0; i < attempts; i++)
        {
            var resp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
            if (resp.StatusCode != HttpStatusCode.TooManyRequests) continue;
            got429 = true;
            break;
        }

        Assert.True(got429);
    }
}