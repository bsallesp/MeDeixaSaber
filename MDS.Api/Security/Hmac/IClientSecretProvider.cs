namespace MDS.Api.Security.Hmac;

public interface IClientSecretProvider
{
    bool TryGetSecret(string apiKey, out string secret);
}