using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MDS.Api.Tests;

public class WebAppFactoryRateLimited : WebApplicationFactory<Program>
{
    public WebAppFactoryRateLimited() { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "iss",
                ["Jwt:Audience"] = "aud",
                ["Jwt:SigningKey"] = new string('k', 32),
                ["Jwt:ExpirationMinutes"] = "60"
            };
            cfg.AddInMemoryCollection(dict);
        });
    }
}