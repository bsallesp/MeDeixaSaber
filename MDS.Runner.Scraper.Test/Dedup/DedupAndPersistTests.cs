using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using MDS.Runner.Scraper.Services;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Test.Dedup;

public sealed class DedupAndPersistTests
{
    Classified C(string title, string? desc, string captured) => new()
    {
        Title = title,
        Description = desc,
        CapturedAtUtc = string.IsNullOrWhiteSpace(captured) ? null : DateTime.Parse(captured)
    };

    [Fact]
    public void Key_Normalizes_Title_Description_And_Date()
    {
        var c1 = C("  APTO  LUXO \n", "Desc \r\n X", "2025-08-21T10:00:00Z");
        var c2 = C("apto luxo", "desc  x", "2025-08-21T10:00:00Z");

        var k1 = DedupAndPersist.Key(c1);
        var k2 = DedupAndPersist.Key(c2);

        k1.Should().Be(k2);
    }

    [Fact]
    public async Task UpsertNewAsync_Inserts_Only_NonExisting()
    {
        var existing = new[]
        {
            C("Apto 2Q Boca", "perto da praia", "2025-08-20T00:00:00Z")
        };
        var scraped = new[]
        {
            C("Apto 2Q Boca", "perto  da  praia", "2025-08-20T00:00:00Z"),
            C("Casa 3Q Deerfield", "com piscina", "2025-08-20T00:00:00Z"),
            C("Apto 2Q Boca", "perto da praia", "2025-08-21T00:00:00Z")
        };

        var repo = new FakeRepoDedup(existing);
        var svc = new DedupAndPersist(repo);

        var inserted = await svc.UpsertNewAsync(scraped, new DateTime(2025, 8, 20));

        inserted.Should().Be(2);
        repo.Inserts.Select(x => x.Title).Should().BeEquivalentTo(new[] { "Casa 3Q Deerfield", "Apto 2Q Boca" });
    }
}