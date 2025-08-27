using System.Net.Http;
using FluentAssertions;
using MDS.Runner.Scraper.Services;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using MDS.Data.Repositories;
using MDS.Data.Repositories.Interfaces;

namespace MDS.Runner.Scraper.Test.Orchestration;

file sealed class FakeScraper(string fileToReturn) : IScraper
{
    public Task<string> RunAsync(HttpClient http, string dateStr) => Task.FromResult(fileToReturn);
}

file sealed class FakeReader : IScrapedCsvReader
{
    readonly Dictionary<string, IEnumerable<Classified>> _byPath;
    public FakeReader(Dictionary<string, IEnumerable<Classified>> byPath) => _byPath = byPath;
    public IEnumerable<Classified> Load(string path) => _byPath.TryGetValue(path, out var v) ? v : Array.Empty<Classified>();
}

file sealed class FakeRepo(IEnumerable<Classified> existing) : IClassifiedsRepository
{
    public readonly List<Classified> Inserts = new();
    public Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc) => Task.FromResult(existing);
    public Task<IEnumerable<Classified>> GetLatestAsync(int take = 50) => Task.FromResult<IEnumerable<Classified>>(Array.Empty<Classified>());
    public Task InsertAsync(Classified entity) { Inserts.Add(entity); return Task.CompletedTask; }
}

file sealed class FakeUploader : IStorageUploader
{
    public readonly List<(string src, string file, CancellationToken ct)> Saves = new();
    public Task SaveAsync(string source, string path, CancellationToken cancellationToken = default)
    {
        Saves.Add((source, path, cancellationToken));
        return Task.CompletedTask;
    }
}

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
        var orchestrator = new ScrapeOrchestrator(
            new FakeScraper("f1"),
            new FakeScraper("f2"),
            repo,
            new TitleNormalizationService(),
            new ClassifiedsFilter(new TitleNormalizationService()),
            reader,
            uploader
        );

        using var http = new HttpClient();
        var inserted = await orchestrator.RunForDateAsync(http, new DateTime(2025, 8, 21, 0, 0, 0, DateTimeKind.Utc), doUpload: true);

        inserted.Should().Be(2);
        repo.Inserts.Should().HaveCount(2);
        repo.Inserts.Select(x => (x.Title, x.PostDate)).Should().BeEquivalentTo([
            ("Casa 3Q Deerfield", DateTime.Parse("2025-08-20").Date),
            ("Apto 2Q Boca",     DateTime.Parse("2025-08-21").Date)
        ]);
        uploader.Saves.Select(s => (s.src, s.file)).Should().BeEquivalentTo([
            ("acheiusa","f1"),
            ("opajuda","f2")
        ]);
    }

    [Fact]
    public async Task RunForDate_NoUpload_When_Disabled()
    {
        var reader = new FakeReader(new()
        {
            ["f1"] = [C("X", "2025-08-21")],
            ["f2"] = [C("Y", "2025-08-21")]
        });

        var repo = new FakeRepo([]);
        var orchestrator = new ScrapeOrchestrator(
            new FakeScraper("f1"),
            new FakeScraper("f2"),
            repo,
            new TitleNormalizationService(),
            new ClassifiedsFilter(new TitleNormalizationService()),
            reader,
            uploader: null
        );

        using var http = new HttpClient();
        var inserted = await orchestrator.RunForDateAsync(http, new DateTime(2025, 8, 21, 0, 0, 0, DateTimeKind.Utc), doUpload: false);

        inserted.Should().Be(2);
        repo.Inserts.Should().HaveCount(2);
    }
}
