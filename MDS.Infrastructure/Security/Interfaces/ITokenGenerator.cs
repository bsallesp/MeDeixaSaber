using System.Security.Claims;

namespace MDS.Infrastructure.Security.Interfaces;

public interface ITokenGenerator
{
    string Generate(IEnumerable<Claim> claims);
}