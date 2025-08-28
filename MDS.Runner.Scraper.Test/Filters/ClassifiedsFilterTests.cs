using FluentAssertions;
using MDS.Runner.Scraper.Services;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Test.Filters;

public sealed class ClassifiedsFilterTests
{
    Classified NewC(string title, string date) => new()
    {
        Title = title,
        PostDate = DateTime.Parse(date).Date
    };

    [Fact]
    public void Filter_Removes_Existing_And_Batch_Duplicates()
    {
        var norm = new FakeNorm();
        var filter = new ClassifiedsFilter(norm);

        var existing = new[]
        {
            NewC("Apto 2Q Boca", "2025-08-20"),
        };

        var scraped = new[]
        {
            NewC("Apto 2Q Boca", "2025-08-20"),
            NewC("apto 2q  boca ", "2025-08-20"),
            NewC("Casa 3Q Deerfield", "2025-08-20"),
            NewC("Casa 3Q Deerfield", "2025-08-20"),
            NewC("Apto 2Q Boca", "2025-08-21")
        };

        var r = filter.Filter(scraped, existing);
        r.Select(x => (x.Title, x.PostDate)).Should().BeEquivalentTo(new (string, DateTime)[]
        {
            ("Casa 3Q Deerfield", DateTime.Parse("2025-08-20").Date),
            ("Apto 2Q Boca", DateTime.Parse("2025-08-21").Date)
        });
    }

    [Fact]
    public void Filter_Treats_Titles_As_Case_Insensitive()
    {
        var norm = new FakeNorm();
        var filter = new ClassifiedsFilter(norm);

        var existing = Array.Empty<Classified>();
        var scraped = new[]
        {
            NewC("APTO LUXO", "2025-08-20"),
            NewC("apto luxo", "2025-08-20"),
        };

        var r = filter.Filter(scraped, existing);

        r.Should().HaveCount(1);
        r[0].Title.Should().Be("APTO LUXO");
    }

    [Fact]
    public void Filter_Allows_Same_Title_On_Different_Dates()
    {
        var norm = new FakeNorm();
        var filter = new ClassifiedsFilter(norm);

        var existing = Array.Empty<Classified>();
        var scraped = new[]
        {
            NewC("Casa Verde", "2025-08-20"),
            NewC("Casa Verde", "2025-08-21"),
        };

        var r = filter.Filter(scraped, existing);

        r.Should().HaveCount(2);
        r.Select(x => x.PostDate).Should().BeEquivalentTo(new[]
        {
            DateTime.Parse("2025-08-20").Date,
            DateTime.Parse("2025-08-21").Date
        });
    }
}
