namespace MDS.Application.Contracts;

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);