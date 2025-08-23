using System.Security.Claims;

namespace MDS.Application.Security.Interfaces;

public interface ITokenService
{
    string GenerateToken(IEnumerable<Claim> claims);
}