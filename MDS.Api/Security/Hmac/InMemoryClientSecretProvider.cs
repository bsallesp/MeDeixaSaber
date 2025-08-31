using System.Collections.Concurrent;

namespace MDS.Api.Security.Hmac;

public sealed class InMemoryClientSecretProvider : IClientSecretProvider
{
    readonly ConcurrentDictionary<string, string> _secrets = new();

    public InMemoryClientSecretProvider()
    {
        _secrets.TryAdd("demo-key", "demo-secret");
    }

    public bool TryGetSecret(string apiKey, out string secret)
    {
        var ok = _secrets.TryGetValue(apiKey, out var s);
        secret = s ?? string.Empty;
        return ok;
    }
}