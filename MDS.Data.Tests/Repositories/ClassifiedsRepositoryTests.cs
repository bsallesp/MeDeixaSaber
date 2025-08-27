using System.Reflection;
using FluentAssertions;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MDS.Data.Tests.Repositories;

public class ClassifiedsRepositoryTests
{
    [Fact]
    public async Task GetLatestAsync_WithNonPositiveTake_ShouldThrow()
    {
        var repo = new ClassifiedsRepository(
            new FakeFactory(() => new ThrowingConnection(new Exception())),
            new FakeNormalizer(""),
            NullLogger<ClassifiedsRepository>.Instance);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repo.GetLatestAsync(0));
    }

    [Fact]
    public async Task InsertAsync_WithNull_ShouldThrow()
    {
        var repo = new ClassifiedsRepository(
            new FakeFactory(() => new ThrowingConnection(new Exception())),
            new FakeNormalizer(""),
            NullLogger<ClassifiedsRepository>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() => repo.InsertAsync(null!));
    }

    [Fact]
    public async Task InsertAsync_DuplicateKey_ShouldThrowInvalidOperationWithMessage()
    {
        var sqlEx = CreateSqlException(2627);
        var repo = new ClassifiedsRepository(
            new FakeFactory(() => new ThrowingConnection(sqlEx)),
            new FakeNormalizer("_n"),
            NullLogger<ClassifiedsRepository>.Instance);

        var entity = new Classified { Title = "t", PostDate = DateTime.UtcNow };
        var act = async () => await repo.InsertAsync(entity);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        ex.Message.Should().Contain("Duplicate key violation");
        entity.Title.Should().EndWith("_n");
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
                error = (SqlError)ctor.Invoke(args);
                break;
            }
            catch { }
        }
        var collection = Activator.CreateInstance(typeof(SqlErrorCollection), true)!;
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(collection, new object[] { error });
        var exc = typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)!.Invoke(null, new[] { collection, "7.0.0" }) as SqlException;
        return exc!;
    }
}
