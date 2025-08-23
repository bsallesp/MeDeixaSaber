using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace MDS.Api.Tests.Controllers;

public class Auth_InvalidSignature_Tests(WebAppFactory f) : IClassFixture<WebAppFactory>
{
    readonly HttpClient _client = f.CreateClient();

    [Fact]
    public async Task Me_WithBadSignature_Should401()
    {
        var badKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 32)));
        var creds = new SigningCredentials(badKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "iss",
            audience: "aud",
            claims: new[] { new Claim(JwtRegisteredClaimNames.Sub, "mallory") },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var resp = await _client.GetAsync("/api/auth/me");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}