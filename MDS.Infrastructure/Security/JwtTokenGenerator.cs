using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MDS.Application.Security;
using MDS.Application.Time;
using MDS.Infrastructure.Security.Interfaces;

namespace MDS.Infrastructure.Security;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options, IClock clock) : ITokenGenerator
{
    readonly IOptions<JwtOptions> _options = options;
    readonly IClock _clock = clock;

    public string Generate(IEnumerable<Claim> claims)
    {
        var o = _options.Value;

        var now = _clock.UtcNow;
        var notBefore = now.AddMinutes(-1);

        var list = new List<Claim>(claims)
        {
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(notBefore).ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Nbf, new DateTimeOffset(notBefore).ToUnixTimeSeconds().ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: o.Issuer,
            audience: o.Audience,
            claims: list,
            notBefore: notBefore,
            expires: now.AddMinutes(o.ExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}