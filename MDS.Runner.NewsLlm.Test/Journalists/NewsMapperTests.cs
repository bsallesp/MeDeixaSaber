using FluentAssertions;
using MDS.Infrastructure.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MDS.Runner.NewsLlm.Journalists;
using MeDeixaSaber.Core.Models;
using Xunit;

namespace MDS.Runner.NewsLlm.Test.Journalists
{
    public sealed class NewsMapperTests
    {
        readonly NewsMapper _sut = new();

        [Fact]
        public void Map_NullArticle_Throws()
        {
            Action act = () => _sut.Map(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Map_MissingTitle_ReturnsNull()
        {
            var article = new NewsArticleDto { Url = "https://x" };
            var result = _sut.Map(article);
            result.Should().BeNull();
        }

        [Fact]
        public void Map_MissingUrl_ReturnsNull()
        {
            var article = new NewsArticleDto { Title = "t" };
            var result = _sut.Map(article);
            result.Should().BeNull();
        }

        [Fact]
        public void Map_Valid_MapsFields()
        {
            var dt = DateTime.UtcNow.AddHours(-1);
            var article = new NewsArticleDto
            {
                Title = "t",
                Description = "d",
                Content = "c",
                Url = "https://x",
                PublishedAt = dt,
                Source = new NewsSourceDto { Name = "S" }
            };

            var result = _sut.Map(article)!;

            result.Title.Should().Be("t");
            result.Summary.Should().Be("d");
            result.Content.Should().Be("c");
            result.Source.Should().Be("S");
            result.Url.Should().Be("https://x");
            result.PublishedAt.Should().Be(dt);
        }

        [Fact]
        public void Map_SourceNameNull_UsesUnknown()
        {
            var article = new NewsArticleDto
            {
                Title = "t",
                Url = "https://x",
                Source = new NewsSourceDto { Name = null }
            };

            var result = _sut.Map(article)!;
            result.Source.Should().Be("(unknown)");
        }

        [Fact]
        public void Map_PublishedAtDefault_UsesUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-2);
            var article = new NewsArticleDto
            {
                Title = "t",
                Url = "https://x",
                PublishedAt = default
            };

            var result = _sut.Map(article)!;
            var after = DateTime.UtcNow.AddSeconds(2);

            result.PublishedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }
    }
}
