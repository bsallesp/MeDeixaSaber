using FluentAssertions;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Application;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;
using Moq;
using Xunit;

namespace MDS.Runner.NewsLlm.Test.Program
{
    public sealed class AppRunnerTests
    {
        [Fact]
        public async Task RunAsync_WhenCollectorReturnsNull_ReturnsZero_AndDoesNotPersist()
        {
            var collector = new Mock<INewsOrgCollector>();
            collector.Setup(c => c.RunAsync("endpoint-newsapi-org-everything", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((NewsApiResponse?)null);

            var journalist = new Mock<IJournalist>(MockBehavior.Strict);
            var sink = new Mock<IArticleSink>(MockBehavior.Strict);

            var sut = new AppRunner(
                collector.Object,
                Mock.Of<MDS.Runner.NewsLlm.Journalists.IOpenAiNewsRewriter>(),
                Mock.Of<MDS.Runner.NewsLlm.Journalists.INewsMapper>(),
                journalist.Object,
                sink.Object);

            var count = await sut.RunAsync();

            count.Should().Be(0);
            collector.VerifyAll();
            sink.VerifyNoOtherCalls();
            journalist.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task RunAsync_WhenOk_RewritesAndPersistsOneByOne_ReturnsCount()
        {
            var payload = new NewsApiResponse
            {
                Articles = [ new NewsArticle { Title = "t", Url = "https://x" } ]
            };

            var rewritten = new List<News>
            {
                new() { Title = "rt1", Content = "c", Source = "S", Url = "https://x/1", PublishedAt = DateTime.UtcNow }
            };

            var collector = new Mock<INewsOrgCollector>();
            collector.Setup(c => c.RunAsync("endpoint-newsapi-org-everything", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(payload);

            var journalist = new Mock<IJournalist>();
            journalist.Setup(j => j.StreamWriteAsync(
                    It.IsAny<NewsApiResponse>(),
                    It.IsAny<EditorialBias>(),
                    It.IsAny<CancellationToken>()))
                .Returns((NewsApiResponse _, EditorialBias _, CancellationToken _) => GetAsync(rewritten));

            static async IAsyncEnumerable<News> GetAsync(IEnumerable<News> items)
            {
                foreach (var i in items)
                {
                    yield return i;
                    await Task.Yield();
                }
            }

            var sink = new Mock<IArticleSink>();
            sink.Setup(s => s.InsertAsync(It.Is<News>(n => n.Title == "rt1")))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var sut = new AppRunner(
                collector.Object,
                Mock.Of<MDS.Runner.NewsLlm.Journalists.IOpenAiNewsRewriter>(),
                Mock.Of<MDS.Runner.NewsLlm.Journalists.INewsMapper>(),
                journalist.Object,
                sink.Object);

            var count = await sut.RunAsync();

            count.Should().Be(1);
            sink.Verify(s => s.InsertAsync(It.IsAny<News>()), Times.Exactly(1));
            collector.VerifyAll();
            journalist.VerifyAll();
            sink.VerifyAll();
        }
    }
}
