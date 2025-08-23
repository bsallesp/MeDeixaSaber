using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MDS.Application.Security;
using MDS.Application.Time;
using MDS.Infrastructure.Security;
using Xunit;

namespace MDS.Infrastructure.Tests.Security
{
    public sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    public class JwtTokenGeneratorTests
    {
        private static JwtOptions ValidOptions => new JwtOptions
        {
            Issuer = "iss",
            Audience = "aud",
            SigningKey = new string('k', 32),
            ExpirationMinutes = 60
        };

        private static IEnumerable<Claim> SampleClaims =>
            [new Claim(ClaimTypes.NameIdentifier, "123"), new Claim(ClaimTypes.Name, "alice")];

        [Fact]
        public void Generate_ShouldProduce_ValidToken()
        {
            var clock = new FixedClock(DateTime.UtcNow);
            var gen = new JwtTokenGenerator(Options.Create(ValidOptions), clock);

            var jwt = gen.Generate(SampleClaims);
            jwt.Should().NotBeNullOrWhiteSpace();

            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = ValidOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = ValidOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidOptions.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            handler.Invoking(h => h.ValidateToken(jwt, parameters, out _)).Should().NotThrow();
        }

        [Fact]
        public void Generate_WithWrongKey_ShouldFailValidation()
        {
            var clock = new FixedClock(DateTime.UtcNow);
            var gen = new JwtTokenGenerator(Options.Create(ValidOptions), clock);
            var jwt = gen.Generate(SampleClaims);

            var handler = new JwtSecurityTokenHandler();
            var badParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('x', 32))),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            handler.Invoking(h => h.ValidateToken(jwt, badParams, out _))
                   .Should().Throw<SecurityTokenInvalidSignatureException>();
        }

        [Fact]
        public void Generate_ShouldRespectClock_ForNbfAndIat()
        {
            var fixedNow = new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
            var clock = new FixedClock(fixedNow);
            var gen = new JwtTokenGenerator(Options.Create(ValidOptions), clock);

            var jwt = gen.Generate(SampleClaims);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);

            var iat = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Iat).Value;
            var nbf = token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Nbf).Value;

            // O gerador usa now.AddMinutes(-1)
            var expected = new DateTimeOffset(fixedNow.AddMinutes(-1)).ToUnixTimeSeconds().ToString();

            iat.Should().Be(expected);
            nbf.Should().Be(expected);
        }
    }
}
