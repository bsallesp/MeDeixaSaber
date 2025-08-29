using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MeDeixaSaber.Core.Models;
using MDS.Runner.NewsLlm.Journalists;
using Xunit;

namespace MDS.Tests.Journalists
{
    public sealed class OpenAiNewsRewriterTests
    {
        sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
        {
            readonly Queue<HttpResponseMessage> _q = new(responses);
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_q.Dequeue());
        }

        static HttpClient Client(params HttpResponseMessage[] responses) => new(new QueueHandler(responses));

        static string PayloadJson(OutsideNews src, string createdAt = "2024-01-01T00:00:00Z") =>
            $$"""
            {
              "Title": "Rewritten Title",
              "Summary": "One line summary.",
              "Content": "Body.",
              "Source": "{{src.Source}}",
              "Url": "{{src.Url}}",
              "PublishedAt": "{{src.PublishedAt.ToUniversalTime():yyyy-MM-ddTHH:mm:ssZ}}",
              "CreatedAt": "{{createdAt}}"
            }
            """;

        static string AsOutputText(string json)
        {
            var s = json.Replace("\\", "\\\\").Replace("\"", "\\\"");
            s = s.Replace("\r", "\\n").Replace("\n", "\\n");
            return "{ \"output_text\": \"" + s + "\" }";
        }

        [Fact]
        public async Task RewriteAsync_HappyPath_ParsesAndOverridesFields()
        {
            var src = new OutsideNews
            {
                Title = "t",
                Summary = "d",
                Content = "c",
                Source = "SRC",
                Url = "https://x",
                PublishedAt = DateTime.UtcNow.AddHours(-1)
            };

            var body = AsOutputText(PayloadJson(src));
            var http = Client(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

            var sut = new OpenAiNewsRewriter(http, "key", "gpt-4o-mini", verbose: false);

            var result = await sut.RewriteAsync(src, EditorialBias.Neutro);

            result.Title.Should().NotBeNullOrWhiteSpace();
            result.Content.Should().NotBeNullOrWhiteSpace();
            result.Source.Should().Be(src.Source);
            result.Url.Should().Be(src.Url);
            result.PublishedAt.Should().Be(src.PublishedAt);
        }

        [Fact]
        public async Task RewriteAsync_NonSuccess_Throws()
        {
            var src = new OutsideNews
            {
                Title = "t",
                Content = "c",
                Source = "SRC",
                Url = "https://x",
                PublishedAt = DateTime.UtcNow
            };

            var http = Client(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("""{"error":"bad"}""")
            });

            var sut = new OpenAiNewsRewriter(http, "key", "gpt-4o-mini", verbose: false);

            var act = async () => await sut.RewriteAsync(src, EditorialBias.Neutro);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RewriteAsync_EmptyOutput_Throws()
        {
            var src = new OutsideNews
            {
                Title = "t",
                Content = "c",
                Source = "SRC",
                Url = "https://x",
                PublishedAt = DateTime.UtcNow
            };

            var http = Client(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"output_text": ""}""")
            });

            var sut = new OpenAiNewsRewriter(http, "key", "gpt-4o-mini", verbose: false);

            var act = async () => await sut.RewriteAsync(src, EditorialBias.Neutro);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task RewriteAsync_FillsCreatedAt_WhenDefault()
        {
            var src = new OutsideNews
            {
                Title = "t",
                Content = "c",
                Source = "SRC",
                Url = "https://x",
                PublishedAt = DateTime.UtcNow
            };

            var body = AsOutputText(PayloadJson(src, "0001-01-01T00:00:00Z"));
            var http = Client(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

            var sut = new OpenAiNewsRewriter(http, "key", "gpt-4o-mini", verbose: false);

            var result = await sut.RewriteAsync(src, EditorialBias.Neutro);

            result.CreatedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        }
    }
}
