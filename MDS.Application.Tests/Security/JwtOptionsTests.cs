using FluentAssertions;
using MDS.Application.Security;
using Xunit;

namespace MDS.Application.Tests.Security
{
    public class JwtOptionsTests
    {
        [Fact]
        public void Defaults_ShouldBeEmptyOrZero()
        {
            var o = new JwtOptions();
            o.Issuer.Should().BeEmpty();
            o.Audience.Should().BeEmpty();
            o.SigningKey.Should().BeEmpty();
            o.ExpirationMinutes.Should().Be(0);
        }

        [Fact]
        public void Setters_ShouldAssignValues()
        {
            var o = new JwtOptions
            {
                Issuer = "iss",
                Audience = "aud",
                SigningKey = "key",
                ExpirationMinutes = 60
            };

            o.Issuer.Should().Be("iss");
            o.Audience.Should().Be("aud");
            o.SigningKey.Should().Be("key");
            o.ExpirationMinutes.Should().Be(60);
        }
    }
}