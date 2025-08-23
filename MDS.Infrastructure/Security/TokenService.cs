using System.Security.Claims;
using MDS.Application.Security;
using MDS.Application.Security.Interfaces;
using MDS.Infrastructure.Security.Interfaces;

namespace MDS.Infrastructure.Security;

public sealed class TokenService(ITokenGenerator generator) : ITokenService
{
    public string GenerateToken(IEnumerable<Claim> claims) => generator.Generate(claims);
}