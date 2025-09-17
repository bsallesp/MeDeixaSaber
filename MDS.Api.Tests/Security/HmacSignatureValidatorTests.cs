using System.Security.Cryptography;
using System.Text;
using MDS.Api.Security.Hmac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;

namespace MDS.Api.Tests.Security;

public sealed class HmacSignatureValidatorTests : IDisposable
{
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly IClientSecretProvider _secretProvider;
    private readonly INonceStore _nonceStore;
    private readonly HmacSignatureValidator _validator;

    public HmacSignatureValidatorTests()
    {
        var options = Options.Create(new HmacSignatureOptions());
        _secretProvider = new InMemoryClientSecretProvider();
        _nonceStore = new MemoryNonceStore(_memoryCache);
        _validator = new HmacSignatureValidator(options, _secretProvider, _nonceStore);
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string path,
        string query,
        string apiKey,
        string timestamp,
        string nonce,
        string signature,
        string body = "")
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = method;
        request.Path = path;
        request.QueryString = new QueryString(query);
        request.Headers["X-Api-Key"] = apiKey;
        request.Headers["X-Timestamp"] = timestamp;
        request.Headers["X-Nonce"] = nonce;
        request.Headers["X-Signature"] = signature;

        if (!string.IsNullOrEmpty(body))
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            request.Body = new MemoryStream(bodyBytes);
        }

        return context;
    }

    private static (string timestamp, string nonce, string signature) GenerateValidHeaders(string method, string path, string query, string body = "")
    {
        const string secret = "demo-secret";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonce = Guid.NewGuid().ToString("N");
        var bodyHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var canonical = $"{method.ToUpperInvariant()}\n{path}\n{query}\n{timestamp}\n{nonce}\n{bodyHash}";
        var sigBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical));
        var signature = Convert.ToHexString(sigBytes);
        return (timestamp, nonce, signature);
    }

    [Fact]
    public async Task ValidateAsync_WithValidSignature_ShouldReturnTrue()
    {
        var (timestamp, nonce, signature) = GenerateValidHeaders("GET", "/api/test", "?take=5");
        var context = CreateContext("GET", "/api/test", "?take=5", "demo-key", timestamp, nonce, signature);

        var result = await _validator.ValidateAsync(context);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidSignature_ShouldReturnFalse()
    {
        var (timestamp, nonce, _) = GenerateValidHeaders("GET", "/api/test", "?take=5");
        var context = CreateContext("GET", "/api/test", "?take=5", "demo-key", timestamp, nonce, "INVALID_SIGNATURE");

        var result = await _validator.ValidateAsync(context);

        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task ValidateAsync_WithReusedNonce_ShouldReturnFalse()
    {
        var (timestamp, nonce, signature) = GenerateValidHeaders("GET", "/api/test", "?take=5");
        var context1 = CreateContext("GET", "/api/test", "?take=5", "demo-key", timestamp, nonce, signature);
        var context2 = CreateContext("GET", "/api/test", "?take=5", "demo-key", timestamp, nonce, signature);
    
        await _validator.ValidateAsync(context1);
        var result = await _validator.ValidateAsync(context2);
    
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_WithExpiredTimestamp_ShouldReturnFalse()
    {
        var expiredTimestamp = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        var (_, nonce, signature) = GenerateValidHeaders("GET", "/api/test", "?take=5");
        var context = CreateContext("GET", "/api/test", "?take=5", "demo-key", expiredTimestamp, nonce, signature);

        var result = await _validator.ValidateAsync(context);

        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _memoryCache.Dispose();
    }
}