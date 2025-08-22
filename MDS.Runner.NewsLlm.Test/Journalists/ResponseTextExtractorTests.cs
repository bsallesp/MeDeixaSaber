using FluentAssertions;
using MDS.Runner.NewsLlm.Journalists;
using Xunit;

namespace MDS.Tests.Journalists
{
    public sealed class ResponseTextExtractorTests
    {
        [Fact]
        public void Extract_WhenOutputText_ReturnsString()
        {
            var json = """{ "output_text": "{\"a\":1}" }""";
            var s = ResponseTextExtractor.Extract(json);
            s.Should().Be("{\"a\":1}");
        }

        [Fact]
        public void Extract_WhenOutputArray_WithTextString_ReturnsString()
        {
            var json = """
                       {
                         "output": [
                           {
                             "content": [
                               { "type": "input_text", "text": "ignore" },
                               { "type": "output_text", "text": "{\"b\":2}" }
                             ]
                           }
                         ]
                       }
                       """;
            var s = ResponseTextExtractor.Extract(json);
            s.Should().Be("{\"b\":2}");
        }

        [Fact]
        public void Extract_WhenOutputArray_WithTextObjectValue_ReturnsString()
        {
            var json = """
                       {
                         "output": [
                           {
                             "content": [
                               { "type": "output_text", "text": { "value": "{\"c\":3}" } }
                             ]
                           }
                         ]
                       }
                       """;
            var s = ResponseTextExtractor.Extract(json);
            s.Should().Be("{\"c\":3}");
        }

        [Fact]
        public void Extract_WhenNoKnownShape_TriesSliceFirstJsonObject()
        {
            var json = "xxx {\"z\":9} yyy";
            var s = ResponseTextExtractor.Extract(json);
            s.Should().Be("{\"z\":9}");
        }

        [Fact]
        public void Extract_InvalidJson_ReturnsNull()
        {
            var s = ResponseTextExtractor.Extract("not-json");
            s.Should().BeNull();
        }
    }
}