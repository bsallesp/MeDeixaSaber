using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Specialized;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;
using Microsoft.Data.SqlClient;
using Xunit;

namespace MDS.Data.Tests.Repositories;

public class ClassifiedsRepositoryTests
{
    [Fact]
    public async Task InsertAsync_Throws_InvalidOperationException_On_SqlException_2627()
    {
        var sqlException = CreateSqlException(2627);
        var throwingConnection = new ThrowingConnection(sqlException);
        var factory = new FakeFactory(throwingConnection);
        var repository = new ClassifiedsRepository(factory);

        var classified = new Classified { RefId = "123", Title = "Test" };

        Func<Task> act = async () => await repository.InsertAsync(classified);
        
        ExceptionAssertions<InvalidOperationException> assertions = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Duplicate key violation for PostDate: {classified.PostDate}, Title: {classified.Title}");

        assertions.WithInnerException<SqlException>();
    }

    [Fact]
    public async Task InsertAsync_Throws_ArgumentNullException_When_Entity_Is_Null()
    {
        var factory = new FakeFactory(new ThrowingConnection(new Exception()));
        var repository = new ClassifiedsRepository(factory);

        var act = async () => await repository.InsertAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetLatestAsync_Throws_ArgumentOutOfRangeException_When_Take_Is_Zero()
    {
        var factory = new FakeFactory(new ThrowingConnection(new Exception()));
        var repository = new ClassifiedsRepository(factory);

        var act = async () => await repository.GetLatestAsync(0);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }
    
    private static SqlException CreateSqlException(int number)
    {
        var errorCollectionConstructor = typeof(SqlErrorCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        var errorCollection = errorCollectionConstructor.Invoke(null);

        var errorConstructor = typeof(SqlError).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(int), typeof(byte), typeof(byte), typeof(string), typeof(string), typeof(string), typeof(int), typeof(Exception)], null)!;

        var error = errorConstructor.Invoke(new object?[] { number, (byte)0, (byte)0, "server", "message", "proc", 100, null });
        
        var addMethod = typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!;
        addMethod.Invoke(errorCollection, [error]);
        
        var exceptionConstructor = typeof(SqlException).GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null,
            [typeof(SqlErrorCollection), typeof(string)], null)!;
        
        return (SqlException)exceptionConstructor.Invoke(null, [errorCollection, "1.0.0"])!;
    }
}