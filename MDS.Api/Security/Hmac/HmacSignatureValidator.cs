using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MDS.Api.Security.Hmac;

public sealed class HmacSignatureValidator
{
    readonly HmacSignatureOptions _opt;
    readonly IClientSecretProvider _secrets;
    readonly INonceStore _nonces;

    public HmacSignatureValidator(IOptions<HmacSignatureOptions> opt, IClientSecretProvider secrets, INonceStore nonces)
    {
        _opt = opt.Value;
        _secrets = secrets;
        _nonces = nonces;
    }

    public async Task<bool> ValidateAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(_opt.HeaderApiKey, out var apiKeyV)) return false;
        if (!ctx.Request.Headers.TryGetValue(_opt.HeaderSignature, out var sigV)) return false;
        if (!ctx.Request.Headers.TryGetValue(_opt.HeaderTimestamp, out var tsV)) return false;
        if (!ctx.Request.Headers.TryGetValue(_opt.HeaderNonce, out var nonceV)) return false;

        var apiKey = apiKeyV.ToString();
        var signature = sigV.ToString();
        var tsRaw = tsV.ToString();
        var nonce = nonceV.ToString();

        if (!_secrets.TryGetSecret(apiKey, out var secret)) return false;
        if (!long.TryParse(tsRaw, out var ts)) return false;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (Math.Abs(now - ts) > _opt.AllowedSkewSeconds) return false;

        var nonceKey = $"{apiKey}:{nonce}";
        if (!_nonces.TryRegister(nonceKey, TimeSpan.FromSeconds(_opt.AllowedSkewSeconds))) return false;

        ctx.Request.EnableBuffering();
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        var bodyBytes = ms.ToArray();
        ctx.Request.Body.Position = 0;

        var bodyHash = Convert.ToHexString(SHA256.HashData(bodyBytes));
        var method = ctx.Request.Method.ToUpperInvariant();
        var path = ctx.Request.Path.ToString();
        var query = ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : string.Empty;
        var canonical = $"{method}\n{path}\n{query}\n{ts}\n{nonce}\n{bodyHash}";
        var sigBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(canonical));
        var expected = Convert.ToHexString(sigBytes);

        return FixedTimeEquals(expected, signature);
    }

    static bool FixedTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var res = 0;
        for (var i = 0; i < a.Length; i++) res |= a[i] ^ b[i];
        return res == 0;
    }
}
