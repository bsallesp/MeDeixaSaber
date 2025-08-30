using System.Net;
using System.Net.Http;
using Xunit;

public sealed class PowFilterTests
{
    [Fact]
    public async Task News_Top_Allows_In_Testing_Without_PoW()
    {
        await using var app = new TestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/news/top");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task News_Top_Requires_PoW_In_NonTesting()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/news/top");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal((HttpStatusCode)428, resp.StatusCode);
    }

    [Fact]
    public async Task News_Top_Invalid_PoW_Returns_401_In_NonTesting()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/news/top");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        req.Headers.Add("X-PoW", $"v1:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:badnonce");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}