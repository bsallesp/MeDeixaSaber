using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace MDS.Api.Tests.Controllers;

public class Auth_TokenClaims_Tests(WebAppFactory f) : IClassFixture<WebAppFactory>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task Token_ShouldContain_Sub_And_Jti()
    {
        var resp = await _client.PostAsJsonAsync("/api/auth/token", new { username = "bob", password = "pw" });
        resp.EnsureSuccessStatusCode();
        var tokJson = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        tokJson.Should().NotBeNull();

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokJson!.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "bob");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public async Task Me_WithExpiredToken_Should401()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 32)));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expired = new JwtSecurityToken(
            issuer: "iss",
            audience: "aud",
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(expired);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var resp = await _client.GetAsync("/api/auth/me");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithNotBeforeFuture_Should401()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 32)));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "iss",
            audience: "aud",
            notBefore: DateTime.UtcNow.AddHours(1),
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var resp = await _client.GetAsync("/api/auth/me");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);
