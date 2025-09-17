using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MDS.Api.Tests;

public sealed class WebAppFactoryNews : WebApplicationFactory<Program>
{
    sealed class SuccessGetTopNewsHandler : IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>
    {
        public Task<IReadOnlyList<OutsideNews>> Handle(GetTopNewsQuery request, CancellationToken ct = default)
        {
            var n = request.PageSize;
            if (n <= 0) n = 0;

            var items = Enumerable.Range(1, n).Select(i => new OutsideNews
            {
                Id = i,
                Title = $"News {i}",
                Summary = $"Summary {i}",
                Content = $"Content {i}",
                Source = "test",
                Url = $"https://example.com/{i}",
                ImageUrl = null,
                PublishedAt = DateTime.UtcNow.AddMinutes(-i),
                CreatedAt = DateTime.UtcNow
            }).ToList();

            return Task.FromResult<IReadOnlyList<OutsideNews>>(items);
        }
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(s =>
        {
            s.AddSingleton<IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>, SuccessGetTopNewsHandler>();
        });
    }
}