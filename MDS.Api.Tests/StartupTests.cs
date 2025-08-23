using FluentAssertions;
using MDS.Application.Security.Interfaces;
using MDS.Infrastructure.Security.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Api.Tests
{
    public class TestFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "iss",
                    ["Jwt:Audience"] = "aud",
                    ["Jwt:SigningKey"] = new string('k', 32),
                    ["Jwt:ExpirationMinutes"] = "60"
                };
                config.AddInMemoryCollection(dict);
            });
        }
    }

    public class StartupTests(TestFactory factory) : IClassFixture<TestFactory>
    {
        [Fact]
        public void Services_ShouldResolve_TokenServices()
        {
            var sp = factory.Services;
            sp.GetRequiredService<ITokenService>().Should().NotBeNull();
            sp.GetRequiredService<ITokenGenerator>().Should().NotBeNull();
        }

        [Fact]
        public void App_ShouldStart_AndCreateClient()
        {
            using HttpClient client = factory.CreateClient();
            client.Should().NotBeNull();
        }
    }
}