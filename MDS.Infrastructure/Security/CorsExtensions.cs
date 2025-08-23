using Microsoft.Extensions.DependencyInjection;

namespace MDS.Infrastructure.Security;

public static class CorsExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, string policyName)
    {
        services.AddCors(o =>
            o.AddPolicy(policyName, b =>
                b.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()));
        return services;
    }
}