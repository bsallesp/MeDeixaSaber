using System.Text;
using MDS.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MDS.Infrastructure.Security;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddJwtAuth(this IServiceCollection services, JwtOptions opt)
    {
        if (services.All(s => s.ServiceType != typeof(JwtOptions)))
            services.AddSingleton(opt);

        if (services.All(s => s.ServiceType != typeof(IAuthenticationService)))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = opt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = opt.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opt.SigningKey)),
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                    o.MapInboundClaims = false;
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                });
        }

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSection = config.GetSection("Jwt");
        services.Configure<JwtOptions>(jwtSection);

        if (!services.Any(s => s.ServiceType == typeof(IAuthenticationService)))
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(o =>
                {
                    var issuer = jwtSection["Issuer"];
                    var audience = jwtSection["Audience"];
                    var key = jwtSection["SigningKey"] ?? jwtSection["Key"];

                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!)),
                        ClockSkew = TimeSpan.FromMinutes(2)
                    };
                    o.MapInboundClaims = false;
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                });
        }

        return services;
    }
}
