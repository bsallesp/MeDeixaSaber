using System.Text;
using MDS.Api.Tests.Support;
using MDS.Application.Security;
using MDS.Application.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MDS.Api.Tests.Controllers;

public class WebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IClock>(new FakeClock());

            // Remove qualquer binding antigo
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(IConfigureOptions<JwtOptions>) ||
                d.ServiceType == typeof(IOptions<JwtOptions>) ||
                d.ServiceType == typeof(JwtOptions)).ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // Injeta JwtOptions consistente
            var opts = new JwtOptions
            {
                Issuer = "iss",
                Audience = "aud",
                SigningKey = new string('k', 32),
                ExpirationMinutes = 60
            };
            services.AddSingleton(Options.Create(opts));
            services.AddSingleton(opts);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = opts.Issuer,
                        ValidateAudience = true,
                        ValidAudience = opts.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                    o.MapInboundClaims = false;
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                });
        });

    }
}
