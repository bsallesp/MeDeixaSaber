using System.Net;
using FluentAssertions;
using MDS.Runner.NewsLlm.Journalists;
using MeDeixaSaber.Core.Models;
using Xunit;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MDS.Runner.NewsLlm.Test.Journalists
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

        static string PayloadJson(OutsideNews src, string createdAt = "2024-01-01T00:00:00Z")
        {
            var longContent = string.Join(" ", Enumerable.Repeat("palavra", 400));
            
            var publishedAtIso = src.PublishedAt!.Value.ToUniversalTime().ToString("O");

            return $$"""
                   {
                     "Title": "Rewritten Title",
                     "Summary": "One line summary.",
                     "Content": "{{longContent}}",
                     "Source": "{{src.Source}}",
                     "Url": "{{src.Url}}",
                     "PublishedAt": "{{publishedAtIso}}",
                     "CreatedAt": "{{createdAt}}"
                   }
                   """;
        }

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
                PublishedAt = DateTime.UtcNow.AddHours(-1),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Categories = new List<Category>()
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
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = new List<Category>()
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
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = new List<Category>()
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
                PublishedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                Categories = new List<Category>()
            };

            // REMOVEMOS A SIMULAÇÃO DE DATA MÍNIMA AQUI PARA QUE O SERIALIZADOR USE O UTCNOW CORRETO
            var body = AsOutputText(PayloadJson(src, DateTime.UtcNow.ToString("O"))); 
            var http = Client(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });

            var sut = new OpenAiNewsRewriter(http, "key", "gpt-4o-mini", verbose: false);
            
            var startTime = DateTime.UtcNow.AddSeconds(-5);

            var result = await sut.RewriteAsync(src, EditorialBias.Neutro);

            result.CreatedAt.Should().NotBeNull("porque o rewriter deve preencher a data de criação se ela for default");
            result.CreatedAt!.Value.Should().BeAfter(startTime, "porque o tempo de criação deve ser preenchido durante a execução do rewriter").And.BeBefore(DateTime.UtcNow.AddSeconds(5));
        }
    }
}