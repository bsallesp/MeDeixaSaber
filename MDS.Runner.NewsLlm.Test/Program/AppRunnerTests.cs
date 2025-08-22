using FluentAssertions;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Journalists;
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
                Mock.Of<IOpenAiNewsRewriter>(),
                Mock.Of<INewsMapper>(),
                journalist.Object,
                sink.Object);

            var count = await sut.RunAsync();

            count.Should().Be(0);
            collector.VerifyAll();
        }

        [Fact]
        public async Task RunAsync_WhenOk_RewritesAndPersists_ReturnsCount()
        {
            var payload = new NewsApiResponse { Articles = [ new NewsArticle { Title = "t", Url = "https://x" } ] };
            var rewritten = new List<News>
            {
                new() { Title = "rt1", Content = "c", Source = "S", Url = "https://x/1", PublishedAt = DateTime.UtcNow }
            };

            var collector = new Mock<INewsOrgCollector>();
            collector.Setup(c => c.RunAsync("endpoint-newsapi-org-everything", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(payload);

            var journalist = new Mock<IJournalist>();
            journalist.Setup(j => j.WriteAsync(It.IsAny<NewsApiResponse>(), It.IsAny<EditorialBias>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(rewritten);

            var sink = new Mock<IArticleSink>();
            sink.Setup(s => s.InsertManyAsync(It.Is<IEnumerable<News>>(n => n.SequenceEqual(rewritten))))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var sut = new AppRunner(
                collector.Object,
                Mock.Of<IOpenAiNewsRewriter>(),
                Mock.Of<INewsMapper>(),
                journalist.Object,
                sink.Object);

            var count = await sut.RunAsync();

            count.Should().Be(1);
            sink.Verify();
        }
    }
}
