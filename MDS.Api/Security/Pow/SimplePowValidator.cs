using System.Security.Cryptography;
using System.Text;

namespace MDS.Api.Security.Pow;

public sealed class SimplePowValidator : IPowValidator
{
    public bool IsValid(string token, int difficulty, long nowUnix, int allowedSkewSeconds)
    {
        var parts = token.Split(':');
        if (parts.Length != 3) return false;
        if (!long.TryParse(parts[1], out var ts)) return false;
        if (Math.Abs(nowUnix - ts) > allowedSkewSeconds) return false;
        var nonce = parts[2];
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var bits = LeadingZeroBits(hash);
        return bits >= difficulty && nonce.Length >= 8;
    }

    static int LeadingZeroBits(byte[] data)
    {
        var bits = 0;
        foreach (var b in data)
        {
            if (b == 0) { bits += 8; continue; }
            var v = b;
            while ((v & 0x80) == 0) { bits++; v <<= 1; }
            break;
        }
        return bits;
    }
}