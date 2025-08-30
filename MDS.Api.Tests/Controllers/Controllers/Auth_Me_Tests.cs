using System.Net;
using System.Net.Http;
using Xunit;

public sealed class Auth_Me_Tests
{
    [Fact]
    public async Task Me_Without_Token_Is_401()
    {
        await using var app = new NonTestingFactory();
        var client = app.CreateClient();
        var req = Req.New(client, "/api/auth/me");
        req.Headers.TryAddWithoutValidation("User-Agent", "Tests/1.0");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}