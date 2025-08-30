using System.Net;
using System.Net.Http;
using Xunit;

public sealed class Classifieds_ByDay_Tests
{
    [Fact]
    public async Task Invalid_Day_Returns_400()
    {
        await using var app = new TestingFactory();
        var client = app.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/classifieds/by-day?day=bad");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Valid_Day_Ok()
    {
        await using var app = new TestingFactory();
        var client = app.CreateClient();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var req = new HttpRequestMessage(HttpMethod.Get, $"/api/classifieds/by-day?day={today}&take=5&skip=0");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}