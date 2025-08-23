using System.ComponentModel.DataAnnotations;

namespace MDS.Application.Security;

public sealed class JwtOptions
{
    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Required]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpirationMinutes { get; init; }
}