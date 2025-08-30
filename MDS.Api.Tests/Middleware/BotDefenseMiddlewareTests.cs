using System.Net;
using System.Net.Http;
using Xunit;

public sealed class BotDefenseMiddlewareTests
{
    [Fact]
    public async Task Missing_UserAgent_In_NonTesting_Returns_400()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/classifieds/top");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Take_Too_Large_Is_Rejected_Before_Action()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/classifieds/top?take=100&skip=0");
        TestAuthHeaders.AddHmacHeaders(req);
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}