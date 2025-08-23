using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MDS.Api.Tests.Controllers;

public class AuthControllerTests : IClassFixture<WebAppFactory>
{
    private readonly ITestOutputHelper _testOutputHelper;
    readonly HttpClient _client;

    public AuthControllerTests(WebAppFactory factory, ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IssueToken_ReturnsToken()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "u", password = "p" });
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        doc.RootElement.GetProperty("expiresAt").GetDateTime().Should().BeAfter(DateTime.UtcNow);
        
        _testOutputHelper.WriteLine("Generated token: " + doc.RootElement.GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task Me_ReturnsClaims_WithValidToken()
    {
        var tokenResp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "alice", password = "x" });
        tokenResp.EnsureSuccessStatusCode();
        var tokJson = await tokenResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokJson);
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        // 🔍 Debug temporário
        _testOutputHelper.WriteLine("ACCESS TOKEN RAW JSON: " + tokJson);
        _testOutputHelper.WriteLine("ACCESS TOKEN EXTRACTED: " + token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await _client.GetAsync("/api/auth/me");

        me.EnsureSuccessStatusCode();
        var meJson = await me.Content.ReadAsStringAsync();
        meJson.Should().Contain("alice");
    }
}