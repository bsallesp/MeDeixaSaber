using MDS.Application.Abstractions.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Infrastructure.Integrations.NewsApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNewsApi(this IServiceCollection services, IConfiguration config, string sectionName = "Integrations:NewsApi")
    {
        services.Configure<NewsApiOptions>(config.GetSection(sectionName));

        var tmp = new NewsApiOptions();
        config.GetSection(sectionName).Bind(tmp);

        if (!string.IsNullOrWhiteSpace(tmp.BaseUrl))
        {
            services.AddHttpClient<NewsApiClient>(c =>
            {
                c.BaseAddress = new Uri(tmp.BaseUrl.TrimEnd('/') + "/");
            });

            services.AddSingleton<NewsApiMapper>();
            services.AddSingleton<INewsProvider, NewsApiClient>();
        }

        return services;
    }
}
