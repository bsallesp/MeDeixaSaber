using System.Net;
using System.Net.Http;
using Xunit;

public sealed class HmacAuthFilterTests
{
    [Fact]
    public async Task Classifieds_Top_Allowed_In_Testing_Without_Hmac()
    {
        await using var app = new TestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/classifieds/top");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Classifieds_Top_Requires_Hmac_In_NonTesting()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/classifieds/top");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Classifieds_Top_Accepts_Valid_Hmac_In_NonTesting()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/classifieds/top?take=5&skip=0");
        TestAuthHeaders.AddHmacHeaders(req);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }
}