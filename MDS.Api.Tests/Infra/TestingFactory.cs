using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using MDS.Api.Tests.Infra;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MDS.Application.Abstractions.Data;

public sealed class TestingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
        {
            s.AddScoped<IClassifiedsUnifiedReadRepository, FakeClassifiedsRepo>();
        });
    }
}

public sealed class NonTestingFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:SigningKey"] = new string('k', 32),
                ["Jwt:ExpirationMinutes"] = "60"
            };
            cfg.AddInMemoryCollection(dict);
        });
        builder.ConfigureServices(s =>
        {
            s.AddScoped<IClassifiedsUnifiedReadRepository, FakeClassifiedsRepo>();

            var issuer = "iss";
            var audience = "aud";
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 32)));

            s.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
             .AddJwtBearer(o =>
             {
                 o.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidIssuer = issuer,
                     ValidateAudience = true,
                     ValidAudience = audience,
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = signingKey,
                     ValidateLifetime = true,
                     ClockSkew = TimeSpan.FromMinutes(2)
                 };
                 o.MapInboundClaims = false;
                 o.RequireHttpsMetadata = false;
                 o.SaveToken = true;
             });
        });
    }
}

public static class TestAuthHeaders
{
    public static void AddHmacHeaders(HttpRequestMessage req, string apiKey = "demo-key", string secret = "demo-secret", string nonce = "abc12345")
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var method = req.Method.Method.ToUpperInvariant();
        var raw = req.RequestUri?.ToString() ?? "/";
        string path, query;
        if (req.RequestUri is { IsAbsoluteUri: true })
        {
            path = req.RequestUri!.AbsolutePath;
            query = req.RequestUri!.Query ?? string.Empty;
        }
        else
        {
            var qIdx = raw.IndexOf('?', StringComparison.Ordinal);
            path = qIdx >= 0 ? raw[..qIdx] : raw;
            query = qIdx >= 0 ? raw[qIdx..] : string.Empty;
        }
        Span<byte> bodyBytes = stackalloc byte[0];
        var bodyHash = Convert.ToHexString(SHA256.HashData(bodyBytes));
        var canonical = $"{method}\n{path}\n{query}\n{ts}\n{nonce}\n{bodyHash}";
        var sigBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical));
        var signature = Convert.ToHexString(sigBytes);
        req.Headers.Add("X-Api-Key", apiKey);
        req.Headers.Add("X-Timestamp", ts);
        req.Headers.Add("X-Nonce", nonce);
        req.Headers.Add("X-Signature", signature);
        req.Headers.UserAgent.Add(new ProductInfoHeaderValue("Tests", "1.0"));
    }
}

public static class Req
{
    public static HttpRequestMessage New(HttpClient client, string pathAndQuery, HttpMethod? method = null)
        => new HttpRequestMessage(method ?? HttpMethod.Get, new Uri(client.BaseAddress!, pathAndQuery));
}
