using MDS.Application.Abstractions.Data;
using MeDeixaSaber.Core.Models;

namespace MDS.Api.Tests.Support;

public sealed class FakeOutsideNewsReadRepository : IOutsideNewsReadRepository
{
    public Task<IReadOnlyList<OutsideNews>> GetTopAsync(int pageSize, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var list = Enumerable.Range(1, pageSize).Select(i => new OutsideNews
        {
            Id = i,
            Title = $"News {i}",
            Summary = $"Summary {i}",
            Content = $"Content {i}",
            Source = "fake-repo",
            Url = $"https://example.com/{i}",
            ImageUrl = null,
            PublishedAt = now.AddMinutes(-i),
            CreatedAt = now
        }).ToList();
        return Task.FromResult<IReadOnlyList<OutsideNews>>(list);
    }
}
