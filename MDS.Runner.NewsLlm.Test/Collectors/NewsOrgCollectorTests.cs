using System.Net;
using FluentAssertions;
using MDS.Runner.NewsLlm.Collectors;
using MDS.Runner.NewsLlm.Persisters;
using Moq;
using Xunit;

namespace MDS.Runner.NewsLlm.Test.Collectors
{
    public sealed class NewsOrgCollectorTests
    {
        sealed class FakeHandler(HttpResponseMessage resp) : HttpMessageHandler
        {
            readonly HttpResponseMessage _resp = resp;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_resp);
        }

        [Fact]
        public async Task RunAsync_InvalidSecret_Throws()
        {
            var secrets = new Mock<ISecretReader>();
            var saver = new Mock<IBlobSaver>();
            var http = new HttpClient(new FakeHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            }));

            var sut = new NewsOrgCollector(secrets.Object, saver.Object, http);
            await Assert.ThrowsAsync<ArgumentException>(() => sut.RunAsync("", CancellationToken.None));
        }

        [Fact]
        public async Task RunAsync_HttpOk_ParsesAndSaves()
        {
            var secrets = new Mock<ISecretReader>();
            secrets.Setup(s => s.GetAsync("endpoint", It.IsAny<CancellationToken>()))
                   .ReturnsAsync("https://any");

            var saver = new Mock<IBlobSaver>();
            saver.Setup(s => s.SaveJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Uri("https://blob/uri"));

            var body = @"{
              ""status"": ""ok"",
              ""totalResults"": 1,
              ""articles"": [
                {
                  ""source"": { ""id"": ""v"", ""name"": ""Verge"" },
                  ""title"": ""t"",
                  ""url"": ""https://x"",
                  ""publishedAt"": ""2024-01-01T00:00:00Z"",
                  ""content"": ""c""
                }
              ]
            }";

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body)
            };

            var http = new HttpClient(new FakeHandler(resp));
            var sut = new NewsOrgCollector(secrets.Object, saver.Object, http);

            var result = await sut.RunAsync("endpoint");

            result.Should().NotBeNull();
            result!.Status.Should().Be("ok");
            result.TotalResults.Should().Be(1);
            result.Articles.Should().HaveCount(1);
            result.Articles[0].Title.Should().Be("t");

            saver.Verify(s => s.SaveJsonAsync(It.IsAny<string>(), "newsapi-everything", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_HttpError_SavesWithErrorPrefix()
        {
            var secrets = new Mock<ISecretReader>();
            secrets.Setup(s => s.GetAsync("endpoint", It.IsAny<CancellationToken>()))
                   .ReturnsAsync("https://any");

            var saver = new Mock<IBlobSaver>();
            saver.Setup(s => s.SaveJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Uri("https://blob/uri"));

            var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{}")
            };

            var http = new HttpClient(new FakeHandler(resp));
            var sut = new NewsOrgCollector(secrets.Object, saver.Object, http);

            var result = await sut.RunAsync("endpoint");

            result.Should().NotBeNull();
            result!.Articles.Should().BeEmpty();
            saver.Verify(s => s.SaveJsonAsync(It.IsAny<string>(), "newsapi-everything-error-400", It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
