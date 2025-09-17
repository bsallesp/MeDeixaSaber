using MDS.Application.Abstractions.Data;
using MeDeixaSaber.Core.Models;

namespace MDS.Api.Tests.Support;

public sealed class FakeOutsideNewsReadRepository : IOutsideNewsReadRepository
{
    private readonly List<OutsideNews> _data;

    public FakeOutsideNewsReadRepository()
    {
        var now = DateTime.UtcNow;
        _data = Enumerable.Range(1, 20).Select(i => new OutsideNews
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
    }

    public Task<IReadOnlyList<OutsideNews>> GetTopAsync(int pageSize, CancellationToken ct = default)
    {
        var result = _data.Take(pageSize).ToList();
        return Task.FromResult<IReadOnlyList<OutsideNews>>(result);
    }

    public Task<OutsideNews?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!int.TryParse(id, out var newsId))
        {
            return Task.FromResult<OutsideNews?>(null);
        }

        var newsItem = _data.FirstOrDefault(n => n.Id == newsId);
        return Task.FromResult(newsItem);
    }
}