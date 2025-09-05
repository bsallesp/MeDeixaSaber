using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Models;
using MDS.Application.News.Queries;
using MDS.Application.Abstractions.Data;
using Microsoft.AspNetCore.Hosting;

public class NewsRelated_StartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    readonly WebApplicationFactory<Program> _factory;
    public NewsRelated_StartupTests(WebApplicationFactory<Program> f)
        => _factory = f.WithWebHostBuilder(b => b.UseEnvironment("Testing"));

    [Fact]
    public void Services_ShouldResolve_Handler_And_Repo()
    {
        using var scope = _factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var handler = sp.GetRequiredService<IQueryHandler<GetLatestRelatedNewsQuery, IReadOnlyList<NewsRow>>>();
        var repo = sp.GetRequiredService<INewsRelatedReadRepository>();
        Assert.NotNull(handler);
        Assert.NotNull(repo);
    }
}