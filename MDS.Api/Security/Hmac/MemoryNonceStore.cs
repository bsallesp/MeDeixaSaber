using Microsoft.Extensions.Caching.Memory;

namespace MDS.Api.Security.Hmac;

public sealed class MemoryNonceStore : INonceStore
{
    readonly IMemoryCache _cache;

    public MemoryNonceStore(IMemoryCache cache) => _cache = cache;

    public bool TryRegister(string key, TimeSpan ttl)
    {
        if (_cache.TryGetValue(key, out _)) return false;
        _cache.Set(key, true, ttl);
        return true;
    }
}