using MDS.Api.Security.Pow;
using FluentAssertions;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace MDS.Api.Tests.Security;

public sealed class SimplePowValidatorTests
{
    private readonly IPowValidator _validator = new SimplePowValidator();

    [Fact]
    public void IsValid_WithValidToken_ShouldReturnTrue()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string token = MintValidToken(12, now);
        var result = _validator.IsValid(token, 12, now, 60);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithExpiredToken_ShouldReturnFalse()
    {
        long expiredTs = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        string token = $"v1:{expiredTs}:nonce12345678";
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = _validator.IsValid(token, 12, now, 60);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithFutureToken_ShouldReturnFalse()
    {
        long futureTs = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        string token = $"v1:{futureTs}:nonce12345678";
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = _validator.IsValid(token, 12, now, 60);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithInsufficientDifficulty_ShouldReturnFalse()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string token = MintValidToken(4, now);
        var result = _validator.IsValid(token, 12, now, 60);
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithMalformedToken_ShouldReturnFalse()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = _validator.IsValid("invalid-token-format", 12, now, 60);
        result.Should().BeFalse();
    }

    private static string MintValidToken(int difficulty, long timestamp)
    {
        while (true)
        {
            var nonce = Guid.NewGuid().ToString("N");
            var token = $"v1:{timestamp}:{nonce}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            if (LeadingZeroBits(hash) < difficulty) continue;
            return token;
        }
    }

    private static int LeadingZeroBits(byte[] data)
    {
        var bits = 0;
        foreach (var b in data)
        {
            if (b == 0)
            {
                bits += 8;
                continue;
            }
            var v = b;
            while ((v & 0x80) == 0)
            {
                bits++;
                v <<= 1;
            }
            break;
        }
        return bits;
    }
}