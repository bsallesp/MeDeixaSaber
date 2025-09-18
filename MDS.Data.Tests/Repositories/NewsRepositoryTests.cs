using System;
using System.Threading.Tasks;
using FluentAssertions;
using MDS.Data.Repositories;
using MeDeixaSaber.Core.Models;
using Xunit;

namespace MDS.Data.Tests.Repositories;

public class NewsRepositoryTests
{
    [Fact]
    public async Task ExistsByUrlAsync_Returns_False_When_Url_Is_Null_Or_Whitespace()
    {
        var factory = new FakeFactory(new ThrowingConnection(new Exception("Should not be called")));
        var repository = new NewsRepository(factory);

        var result = await repository.ExistsByUrlAsync(" ");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InsertAsync_Rethrows_Underlying_Exception_On_Failure()
    {
        var dbException = new InvalidOperationException("DB is down");
        var factory = new FakeFactory(new ThrowingConnection(dbException));
        var repository = new NewsRepository(factory);
        var news = new OutsideNews { Url = "http://test.com" };

        var act = async () => await repository.InsertAsync(news);
        
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("DB is down");
    }
}