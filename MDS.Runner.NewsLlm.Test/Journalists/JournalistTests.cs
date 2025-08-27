using FluentAssertions;
using MDS.Runner.NewsLlm.Journalists;
using MeDeixaSaber.Core.Models;
using Moq;
using Xunit;

namespace MDS.Runner.NewsLlm.Test.Journalists
{
    public sealed class JournalistTests
    {
        static NewsArticle Art(int i) => new()
        {
            Title = $"t{i}",
            Url = $"https://x/{i}",
            PublishedAt = DateTime.UtcNow.AddMinutes(-i),
            Source = new NewsSource { Name = "S" },
            Description = $"d{i}",
            Content = $"c{i}"
        };

        static News NewsOf(int i) => new()
        {
            Title = $"rt{i}",
            Summary = $"rd{i}",
            Content = $"rc{i}",
            Source = "S",
            Url = $"https://x/{i}",
            PublishedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task WriteAsync_NullPayload_Throws()
        {
            var mapper = new Mock<INewsMapper>(MockBehavior.Strict);
            var rewriter = new Mock<IOpenAiNewsRewriter>(MockBehavior.Strict);
            var sut = new Journalist(mapper.Object, rewriter.Object);

            var act = async () => await sut.WriteAsync(null!, EditorialBias.Neutro);
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task WriteAsync_NoArticles_ReturnsEmpty()
        {
            var mapper = new Mock<INewsMapper>(MockBehavior.Strict);
            var rewriter = new Mock<IOpenAiNewsRewriter>(MockBehavior.Strict);
            var sut = new Journalist(mapper.Object, rewriter.Object);

            var payload = new NewsApiResponse { Articles = new List<NewsArticle>() };

            var result = await sut.WriteAsync(payload, EditorialBias.Neutro);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task WriteAsync_TakesUpTo30_MapsAndRewrites()
        {
            var articles = Enumerable.Range(1, 12).Select(Art).ToList();
            var payload = new NewsApiResponse { Articles = articles };

            var mapper = new Mock<INewsMapper>();
            foreach (var i in Enumerable.Range(1, 12))
                mapper.Setup(m => m.Map(It.Is<NewsArticle>(a => a.Title == $"t{i}")))
                    .Returns(new News
                    {
                        Title = $"t{i}",
                        Url = $"https://x/{i}",
                        Content = "c",
                        Source = "S",
                        PublishedAt = DateTime.UtcNow
                    });

            var rewriter = new Mock<IOpenAiNewsRewriter>();
            foreach (var i in Enumerable.Range(1, 12))
                rewriter.Setup(r => r.RewriteAsync(It.Is<News>(n => n.Title == $"t{i}"),
                        EditorialBias.Neutro,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(NewsOf(i));

            var sut = new Journalist(mapper.Object, rewriter.Object);
            var result = await sut.WriteAsync(payload, EditorialBias.Neutro);

            result.Count.Should().Be(12);
        }

        [Fact]
        public async Task WriteAsync_WhenOneRewriteFails_ContinuesOthers()
        {
            var articles = Enumerable.Range(1, 5).Select(Art).ToList();
            var payload = new NewsApiResponse { Articles = articles };

            var mapper = new Mock<INewsMapper>();
            foreach (var i in Enumerable.Range(1, 5))
                mapper.Setup(m => m.Map(It.Is<NewsArticle>(a => a.Title == $"t{i}")))
                    .Returns(new News
                    {
                        Title = $"t{i}",
                        Summary = $"d{i}",
                        Content = $"c{i}",
                        Source = "S",
                        Url = $"https://x/{i}",
                        PublishedAt = DateTime.UtcNow
                    });

            var rewriter = new Mock<IOpenAiNewsRewriter>();
            rewriter.Setup(r => r.RewriteAsync(It.Is<News>(n => n.Title == "t3"),
                    EditorialBias.Neutro,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("boom"));
            foreach (var i in new[] { 1, 2, 4, 5 })
                rewriter.Setup(r => r.RewriteAsync(It.Is<News>(n => n.Title == $"t{i}"),
                        EditorialBias.Neutro,
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(NewsOf(i));

            var sut = new Journalist(mapper.Object, rewriter.Object);

            var result = await sut.WriteAsync(payload, EditorialBias.Neutro);

            result.Count.Should().Be(4);
            result.Select(n => n.Title).Should().NotContain("rt3");
        }
    }
}
