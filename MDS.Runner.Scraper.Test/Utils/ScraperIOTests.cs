using System.Globalization;
using System.Text;
using FluentAssertions;
using MDS.Runner.Scraper.Utils;

namespace MDS.Runner.Scraper.Test.Utils;

public sealed class ScraperIoTests
{
    string NewTempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "mds-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    string NewTempFilePath(string? baseDir = null, string? name = null)
    {
        baseDir ??= NewTempDir();
        name ??= Guid.NewGuid().ToString("N") + ".csv";
        return Path.Combine(baseDir, name);
    }

    [Fact]
    public void GetOutputDir_Uses_OUTPUT_DIR_When_Set()
    {
        var custom = NewTempDir();
        Environment.SetEnvironmentVariable("OUTPUT_DIR", custom);

        var dir = ScraperIO.GetOutputDir();

        dir.Should().Be(custom);
        Directory.Exists(dir).Should().BeTrue();

        Environment.SetEnvironmentVariable("OUTPUT_DIR", null);
    }

    [Fact]
    public void GetOutputDir_Defaults_To_TempPath_When_Not_Set()
    {
        Environment.SetEnvironmentVariable("OUTPUT_DIR", null);
        var dir = ScraperIO.GetOutputDir();
        dir.Should().Be(Path.GetTempPath());
        Directory.Exists(dir).Should().BeTrue();
    }

    [Fact]
    public async Task AppendToFile_Creates_Directory_And_Appends()
    {
        var path = NewTempFilePath();

        await ScraperIO.AppendToFile(path, "a,b,c");
        await ScraperIO.AppendToFile(path, "1,2,3");

        File.Exists(path).Should().BeTrue();
        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        lines.Should().BeEquivalentTo(new[] { "a,b,c", "1,2,3" });
    }

    [Fact]
    public async Task AppendToFile_Is_ThreadSafe_With_Shared_Gate()
    {
        var path = NewTempFilePath();
        var gate = new SemaphoreSlim(1, 1);
        var tasks = Enumerable.Range(0, 100)
            .Select(i => ScraperIO.AppendToFile(path, $"L{i}", gate))
            .ToArray();

        await Task.WhenAll(tasks);

        var lines = (await File.ReadAllLinesAsync(path)).Length;
        lines.Should().Be(100);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("simple", "simple")]
    [InlineData("has,comma", "\"has,comma\"")]
    [InlineData("has\"quote", "\"has\"\"quote\"")]
    [InlineData("multi\nline", "\"multi\nline\"")]
    public void CsvEscape_Works(string? input, string expected)
    {
        ScraperIO.CsvEscape(input).Should().Be(expected);
    }

    [Fact]
    public void Csv_Joins_With_Proper_Escaping()
    {
        var csv = ScraperIO.Csv("a", "has,comma", "has\"q", "line\nbreak");
        csv.Should().Be("a,\"has,comma\",\"has\"\"q\",\"line\nbreak\"");
    }

    [Fact]
    public void NowIso_Is_Utc_Iso8601_Roundtrippable()
    {
        var s = ScraperIO.NowIso();
        s.EndsWith("Z").Should().BeTrue();

        var dt = DateTime.Parse(s, null, DateTimeStyles.RoundtripKind);
        dt.Kind.Should().Be(DateTimeKind.Utc);
        (DateTime.UtcNow - dt).Should().BeLessThan(TimeSpan.FromSeconds(5));
    }
}
