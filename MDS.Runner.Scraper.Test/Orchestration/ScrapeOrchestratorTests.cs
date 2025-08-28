using System.Net.Http;
using FluentAssertions;
using MDS.Runner.Scraper.Services;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using Microsoft.Extensions.Logging;
using MDS.Runner.Scraper.Test.Orchestration.Fakes;

namespace MDS.Runner.Scraper.Test.Orchestration;

public sealed class ScrapeOrchestratorTests
{
    Classified C(string title, string date) => new() { Title = title, PostDate = DateTime.Parse(date).Date };

    [Fact]
    public async Task RunForDate_Inserts_Filtered_And_Uploads_When_Enabled()
    {
        var existing = new[] { C("Apto 2Q Boca", "2025-08-20") };
        var f1Items = new[] { C("Apto 2Q Boca", "2025-08-20"), C("Casa 3Q Deerfield", "2025-08-20") };
        var f2Items = new[] { C("Apto 2Q Boca", "2025-08-21") };

        var reader = new FakeReader(new()
        {
            ["f1"] = f1Items,
            ["f2"] = f2Items
        });

        var repo = new FakeRepo(existing);
        var uploader = new FakeUploader();
        var loggerFactory = LoggerFactory.Create(b => { });
        var orchestrator = new ScrapeOrchestrator(
            new FakeScraper("craigslist", "f1"),
            new FakeScraper("opajuda", "f2"),
            repo,
            new TitleNormalizationService(),
            new ClassifiedsFilter(new TitleNormalizationService()),
            reader,
            uploader,
            loggerFactory
        );

        using var http = new HttpClient();
        var inserted = await orchestrator.RunForDateAsync(http, new DateTime(2025, 8, 21, 0, 0, 0, DateTimeKind.Utc), doUpload: true);

        inserted.Should().Be(2);
        repo.Inserts.Should().HaveCount(2);
        repo.Inserts.Select(x => (x.Title, x.PostDate)).Should().BeEquivalentTo(new (string, DateTime)[]
        {
            ("Casa 3Q Deerfield", DateTime.Parse("2025-08-20").Date),
            ("Apto 2Q Boca",     DateTime.Parse("2025-08-21").Date)
        });
        uploader.Saves.Select(s => (s.src, s.file)).Should().BeEquivalentTo(new (string, string)[]
        {
            ("craigslist","f1"),
            ("opajuda","f2")
        });
    }

    [Fact]
    public async Task RunForDate_NoUpload_When_Disabled()
    {
        var reader = new FakeReader(new()
        {
            ["f1"] = new[] { C("X", "2025-08-21") },
            ["f2"] = new[] { C("Y", "2025-08-21") }
        });

        var repo = new FakeRepo(Array.Empty<Classified>());
        var loggerFactory = LoggerFactory.Create(b => { });
        var orchestrator = new ScrapeOrchestrator(
            new FakeScraper("craigslist", "f1"),
            new FakeScraper("opajuda", "f2"),
            repo,
            new TitleNormalizationService(),
            new ClassifiedsFilter(new TitleNormalizationService()),
            reader,
            uploader: null,
            loggerFactory
        );

        using var http = new HttpClient();
        var inserted = await orchestrator.RunForDateAsync(http, new DateTime(2025, 8, 21, 0, 0, 0, DateTimeKind.Utc), doUpload: false);

        inserted.Should().Be(2);
        repo.Inserts.Should().HaveCount(2);
    }
}
