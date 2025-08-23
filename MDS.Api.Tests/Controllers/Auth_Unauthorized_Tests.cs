using System.Net;
using FluentAssertions;
using Xunit;

namespace MDS.Api.Tests.Controllers;

public class Auth_Unauthorized_Tests(WebAppFactory f) : IClassFixture<WebAppFactory>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task Me_WithoutBearer_Should401()
    {
        var resp = await _client.GetAsync("/api/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}