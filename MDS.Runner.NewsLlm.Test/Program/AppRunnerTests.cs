using FluentAssertions;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Application;
using MDS.Runner.NewsLlm.Journalists.Gemini;
using MeDeixaSaber.Core.Models;
using Moq;
using Xunit;

namespace MDS.Runner.NewsLlm.Test.Program
{
    public sealed class AppRunnerTests
    {
        private readonly Mock<IGeminiJournalistService> _journalistMock;
        private readonly Mock<IArticleSink> _sinkMock;
        private readonly Mock<IArticleRead> _readerMock;
        private readonly Mock<IImageFinder> _imageFinderMock;

        public AppRunnerTests()
        {
            _journalistMock = new Mock<IGeminiJournalistService>();
            _sinkMock = new Mock<IArticleSink>();
            _readerMock = new Mock<IArticleRead>();
            _imageFinderMock = new Mock<IImageFinder>();
        }

        private AppRunner CreateSut()
        {
            return new AppRunner(
                _journalistMock.Object,
                _sinkMock.Object,
                _readerMock.Object,
                _imageFinderMock.Object);
        }

        [Fact]
        public async Task RunAsync_WhenDiscoveryReturnsNoTopics_ReturnsZero()
        {
            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync([]);

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(0);
            _journalistMock.Verify(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Curto")] 
        public async Task RunAsync_WhenResearchDataIsInsufficient_SkipsWritingAndReturnsZero(string? researchData)
        {
            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 1"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync("Headline 1", It.IsAny<CancellationToken>()))
                           .ReturnsAsync(researchData);

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(0);
            _journalistMock.Verify(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _sinkMock.Verify(s => s.InsertAsync(It.IsAny<OutsideNews>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenWritingFails_SkipsArticleAndReturnsZero()
        {
            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 1"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new string('a', 250));
            _journalistMock.Setup(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync((OutsideNews?)null);

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(0);
            _sinkMock.Verify(s => s.InsertAsync(It.IsAny<OutsideNews>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_HappyPath_FindsImageAndPersists_ReturnsOne()
        {
            var newArticle = new OutsideNews 
            { 
                Title = "T1", 
                Url = "https://x.com/1", 
                ImageUrl = null,
                Content = "Content",
                Source = "TestSource",
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = []
            };

            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 1"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new string('a', 250));
            _journalistMock.Setup(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(newArticle);
            _readerMock.Setup(r => r.ExistsByUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            _imageFinderMock.Setup(f => f.FindImageUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync("https://image.com/found.jpg");

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(1);
            _imageFinderMock.Verify(f => f.FindImageUrlAsync("T1", It.IsAny<CancellationToken>()), Times.Once);
            _sinkMock.Verify(s => s.InsertAsync(It.Is<OutsideNews>(n => 
                                   n.ImageUrl == "https://image.com/found.jpg")), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenArticleAlreadyExists_SkipsPersistence()
        {
            var existingArticle = new OutsideNews 
            { 
                Title = "T2", 
                Url = "https://x.com/2", 
                ImageUrl = null,
                Content = "Content",
                Source = "TestSource",
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = []
            };

            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 2"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new string('a', 250));
            _journalistMock.Setup(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(existingArticle);
            _readerMock.Setup(r => r.ExistsByUrlAsync("https://x.com/2")).ReturnsAsync(true);

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(0);
            _imageFinderMock.Verify(f => f.FindImageUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _sinkMock.Verify(s => s.InsertAsync(It.IsAny<OutsideNews>()), Times.Never);
        }

        [Fact]
        public async Task RunAsync_WhenArticleHasImageFromWriter_SkipsImageFinder()
        {
            var preloadedArticle = new OutsideNews 
            { 
                Title = "T3", 
                Url = "https://x.com/3", 
                ImageUrl = "https://writer.com/img.jpg",
                Content = "Content",
                Source = "TestSource",
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = []
            };

            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 3"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new string('a', 250));
            _journalistMock.Setup(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(preloadedArticle);
            _readerMock.Setup(r => r.ExistsByUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            
            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(1);
            _imageFinderMock.Verify(f => f.FindImageUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _sinkMock.Verify(s => s.InsertAsync(It.Is<OutsideNews>(n => 
                                   n.ImageUrl == "https://writer.com/img.jpg")), Times.Once);
        }

        [Fact]
        public async Task RunAsync_WhenImageFinderReturnsNull_PersistsWithoutImage()
        {
            var noImageArticle = new OutsideNews 
            { 
                Title = "T4", 
                Url = "https://x.com/4", 
                ImageUrl = null,
                Content = "Content",
                Source = "TestSource",
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = []
            };

            _journalistMock.Setup(j => j.DiscoverTopicsAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(["Headline 4"]);
            _journalistMock.Setup(j => j.ResearchTopicAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(new string('a', 250));
            _journalistMock.Setup(j => j.WriteArticleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .ReturnsAsync(noImageArticle);
            _readerMock.Setup(r => r.ExistsByUrlAsync(It.IsAny<string>())).ReturnsAsync(false);
            _imageFinderMock.Setup(f => f.FindImageUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync((string?)null);

            var sut = CreateSut();
            var count = await sut.RunAsync();

            count.Should().Be(1);
            _imageFinderMock.Verify(f => f.FindImageUrlAsync("T4", It.IsAny<CancellationToken>()), Times.Once);
            _sinkMock.Verify(s => s.InsertAsync(It.Is<OutsideNews>(n => 
                                   n.ImageUrl == null)), Times.Once);
        }
    }
}