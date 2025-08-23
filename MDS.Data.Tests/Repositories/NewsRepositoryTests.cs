using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;

namespace MDS.Data.Tests.Repositories;

public class NewsRepositoryTests
{
    [Fact]
    public async Task GetByDayAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = new NewsRepository(new FakeFactory(() => new ThrowingConnection(new InvalidOperationException("boom"))));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetByDayAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetTitlesByDayAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = new NewsRepository(new FakeFactory(() => new ThrowingConnection(new InvalidOperationException("fail"))));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetTitlesByDayAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetLatestAsync_WithConnectionException_ShouldThrow()
    {
        var repo = new NewsRepository(new FakeFactory(() => new ThrowingConnection(new Exception("db error"))));
        await Assert.ThrowsAsync<Exception>(() => repo.GetLatestAsync(5));
    }

    [Fact]
    public async Task InsertAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = new NewsRepository(new FakeFactory(() => new ThrowingConnection(new Exception("insert fail"))));
        await Assert.ThrowsAsync<Exception>(() => repo.InsertAsync(new News { Title = "t", Url = "u" }));
    }

    [Fact]
    public async Task InsertManyAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = new NewsRepository(new FakeFactory(() => new ThrowingConnection(new Exception("bulk fail"))));
        var list = new[] { new News { Title = "a", Url = "b" } };
        await Assert.ThrowsAsync<Exception>(() => repo.InsertManyAsync(list));
    }
}