using FluentAssertions;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
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
        public async Task RunAsync_WhenCollectorReturnsNull_ReturnsZero()
        {
            var collector = new Mock<INewsOrgCollector>();
            collector.Setup(c => c.RunAsync("endpoint-newsapi-org-everything", It.IsAny<CancellationToken>()))
                     .ReturnsAsync((NewsApiResponseDto?)null);

            var sut = new AppRunner(
                collector.Object,
                Mock.Of<IOpenAiNewsRewriter>(),
                Mock.Of<INewsMapper>(),
                Mock.Of<IArticleSink>(),
                Mock.Of<IArticleRead>());

            var count = await sut.RunAsync();

            count.Should().Be(0);
        }

        [Fact]
        public async Task RunAsync_WhenOk_RewritesAndPersistsOne_ReturnsCount()
        {
            var payload = new NewsApiResponseDto
            {
                Articles = [ new NewsArticleDto { Title = "t", Url = "https://x" } ]
            };

            var collector = new Mock<INewsOrgCollector>();
            collector.Setup(c => c.RunAsync("endpoint-newsapi-org-everything", It.IsAny<CancellationToken>()))
                     .ReturnsAsync(payload);

            var mapper = new Mock<INewsMapper>();
            mapper.Setup(m => m.Map(It.IsAny<NewsArticleDto>()))
                  .Returns(new OutsideNews { Title = "mapped", Url = "https://x" });

            var rewriter = new Mock<IOpenAiNewsRewriter>();
            rewriter.Setup(r => r.RewriteAsync(It.IsAny<OutsideNews>(), EditorialBias.Neutro, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new OutsideNews { Title = "rewritten", Content = "c", Url = "https://x", Source = "S", PublishedAt = DateTime.UtcNow });

            var reader = new Mock<IArticleRead>();
            reader.Setup(r => r.ExistsByUrlAsync("https://x")).ReturnsAsync(false);

            var sink = new Mock<IArticleSink>();
            sink.Setup(s => s.InsertAsync(It.Is<OutsideNews>(n => n.Title == "rewritten"))).Returns(Task.CompletedTask);

            var sut = new AppRunner(
                collector.Object,
                rewriter.Object,
                mapper.Object,
                sink.Object,
                reader.Object);

            var count = await sut.RunAsync();

            count.Should().Be(1);
            sink.Verify(s => s.InsertAsync(It.IsAny<OutsideNews>()), Times.Once);
        }
    }
}