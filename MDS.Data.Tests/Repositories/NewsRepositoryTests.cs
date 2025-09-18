using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using System.Linq;
using System.Data.Common;

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
        await Assert.ThrowsAsync<Exception>(() => repo.InsertAsync(new OutsideNews { 
            Title = "t", 
            Url = "u", 
            Content = "dummy",
            Source = "test",
            PublishedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            Categories = new List<Category>() 
        }));
    }

    [Fact]
    public async Task InsertManyAsync_WhenConnectionThrows_ShouldPropagate()
    {
        var repo = CreateRepo(() => new ThrowingConnection(new Exception("bulk fail")));
        var list = new[] { 
            new OutsideNews { 
                Title = "a", 
                Url = "b", 
                Content = "dummy",
                Source = "test",
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = new List<Category>() 
            } 
        };
        await Assert.ThrowsAsync<Exception>(() => repo.InsertManyAsync(list));
    }

    static SqlException CreateSqlException(int number)
    {
        var ctors = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        SqlError error = null!;
        foreach (var ctor in ctors)
        {
            var ps = ctor.GetParameters();
            try
            {
                object?[] args = ps.Length switch
                {
                    7 => new object?[] { number, (byte)0, (byte)0, "server", "message", "proc", 0 },
                    8 => new object?[] { number, (byte)0, (byte)0, "server", "message", "proc", 0, null },
                    9 => new object?[] { number, (byte)0, (byte)0, "server", "message", "proc", "source", 0, null },
                    _ => null!
                };
                if (args is null) continue;
                error = (SqlError)ctor.Invoke(args)!;
                break;
            }
            catch { }
        }
        var collection = Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(collection, new object[] { error });
        var exc = typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)!.Invoke(null, new[] { collection, "7.0.0" }) as SqlException;
        return exc!;
    }

    static NewsRepository CreateRepo(Func<DbConnection> factory)
    {
        var type = typeof(NewsRepository);
        var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        foreach (var c in ctors)
        {
            var ps = c.GetParameters();
            switch (ps.Length)
            {
                case 1 when ps[0].ParameterType.Name == "IDbConnectionFactory":
                    return (NewsRepository)Activator.CreateInstance(type, new object[] { new FakeFactory(factory) })!;
                case 2 when ps[0].ParameterType.Name == "IDbConnectionFactory" &&
                            IsLoggerOf(type, ps[1].ParameterType):
                {
                    var logger = GetNullLogger(type);
                    return (NewsRepository)Activator.CreateInstance(type, new object[] { new FakeFactory(factory), logger })!;
                }
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
        var factory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var method = typeof(Microsoft.Extensions.Logging.LoggerFactoryExtensions)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .First(m =>
                m.Name == "CreateLogger" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1);
        var generic = method.MakeGenericMethod(repoType);
        return generic.Invoke(null, [factory])!;
    }
}