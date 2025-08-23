using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;

namespace MDS.Api.Tests.Controllers;

public class Auth_InvalidModel_Tests(WebAppFactory f) : IClassFixture<WebAppFactory>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task IssueToken_MissingBody_Should400()
    {
        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var resp = await _client.PostAsync("/api/auth/token", content);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task IssueToken_EmptyUserOrPass_Should400()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "", password = "" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}