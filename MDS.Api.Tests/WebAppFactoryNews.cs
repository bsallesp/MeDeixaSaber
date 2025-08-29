using MDS.Api.Tests.Support;
using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Api.Tests;

public sealed class WebAppFactoryNews : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<IOutsideNewsReadRepository, FakeOutsideNewsReadRepository>();
            services.AddScoped<IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>, GetTopNewsHandler>();
        });
    }
}
