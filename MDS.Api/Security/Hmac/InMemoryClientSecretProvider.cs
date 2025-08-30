using System.Collections.Concurrent;

namespace MDS.Api.Security.Hmac;

public sealed class InMemoryClientSecretProvider : IClientSecretProvider
{
    readonly ConcurrentDictionary<string, string> _secrets = new();

    public InMemoryClientSecretProvider()
    {
        _secrets.TryAdd("demo-key", "demo-secret");
    }

    public bool TryGetSecret(string apiKey, out string secret) => _secrets.TryGetValue(apiKey, out secret);
}