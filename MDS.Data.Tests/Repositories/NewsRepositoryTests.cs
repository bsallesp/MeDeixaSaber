using System.Data.Common;
using System.Reflection;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MDS.Data.Tests.Repositories;

public class NewsRepositoryTests
{
    [Fact]
    public async Task GetByDayAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new InvalidOperationException("boom")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetByDayAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetTitlesByDayAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new InvalidOperationException("fail")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetTitlesByDayAsync(DateTime.UtcNow));
    }

    [Fact]
    public async Task GetLatestAsync_WithConnectionException_ShouldThrow()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new Exception("db error")));
        await Assert.ThrowsAsync<Exception>(() => repo.GetLatestAsync(5));
    }

    [Fact]
    public async Task InsertAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new Exception("insert fail")));
        await Assert.ThrowsAsync<Exception>(() => repo.InsertAsync(new News { Title = "t", Url = "u" }));
    }

    [Fact]
    public async Task InsertManyAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new Exception("bulk fail")));
        var list = new[] { new News { Title = "a", Url = "b" } };
        await Assert.ThrowsAsync<Exception>(() => repo.InsertManyAsync(list));
    }

    static NewsRepository CreateRepo(Func<DbConnection> factory)
    {
        var type = typeof(NewsRepository);
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var c in ctors)
        {
            var ps = c.GetParameters();
            if (ps.Length == 1 && ps[0].ParameterType.Name == "IDbConnectionFactory")
                return (NewsRepository)Activator.CreateInstance(type, new object[] { new FakeFactory(factory) })!;
            if (ps.Length == 2 && ps[0].ParameterType.Name == "IDbConnectionFactory" &&
                IsLoggerOf(type, ps[1].ParameterType))
            {
                var logger = GetNullLogger(type);
                return (NewsRepository)Activator.CreateInstance(type, new object[] { new FakeFactory(factory), logger })!;
            }
        }

        throw new InvalidOperationException("NewsRepository constructor not found.");
    }

    static bool IsLoggerOf(Type repoType, Type candidate)
    {
        if (!candidate.IsGenericType) return false;
        return candidate.GetGenericTypeDefinition() == typeof(ILogger<>) &&
               candidate.GetGenericArguments()[0] == repoType;
    }

    static object GetNullLogger(Type repoType)
    {
        var nullLoggerType = typeof(NullLogger<>).MakeGenericType(repoType);
        var prop = nullLoggerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)!;
        return prop.GetValue(null)!;
    }
}
