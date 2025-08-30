namespace MDS.Api.Security.Hmac;

public sealed class HmacSignatureOptions
{
    public string HeaderApiKey { get; init; } = "X-Api-Key";
    public string HeaderSignature { get; init; } = "X-Signature";
    public string HeaderTimestamp { get; init; } = "X-Timestamp";
    public string HeaderNonce { get; init; } = "X-Nonce";
    public int AllowedSkewSeconds { get; init; } = 300;
}