namespace MDS.Api.Security.Hmac;

public interface INonceStore
{
    bool TryRegister(string key, TimeSpan ttl);
}