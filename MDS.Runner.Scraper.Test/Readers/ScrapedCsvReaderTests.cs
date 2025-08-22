using FluentAssertions;
using MDS.Runner.Scraper.Services;

namespace MDS.Runner.Scraper.Test.Readers;

public sealed class ScrapedCsvReaderTests
{
    [Fact]
    public void Load_Parses_Valid_Csv()
    {
        var csv = """
                  captured_at_utc,url,title,ref_id,location,when,post_date,phone,state,description
                  2025-08-21T15:30:00Z,https://x/1,Apto Centro,123,Boca Raton,hoje,2025-08-21,5619998888,FL,ótimo apto
                  2025-08-20T10:00:00Z,https://x/2,Casa Deerfield,456,Deerfield,ontem,2025-08-20,,,Casa grande
                  """;

        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, csv);

        var result = ScrapedCsvReader.Load(tmp).ToList();

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Apto Centro");
        
        var expectedUtc = DateTime.Parse(
            "2025-08-21T15:30:00Z",
            null,
            System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
        result[0].CapturedAtUtc.Should().Be(expectedUtc);
        
        result[1].Title.Should().Be("Casa Deerfield");
        result[1].PostDate.Should().Be(DateTime.Parse("2025-08-20"));
    }

    [Fact]
    public void Load_Returns_Empty_For_Empty_File()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "");

        var result = ScrapedCsvReader.Load(tmp).ToList();

        result.Should().BeEmpty();
    }
    
    [Fact]
    public void Load_Parses_Invalid_Dates_As_Null()
    {
        var csv = """
                  captured_at_utc,url,title,ref_id,location,when,post_date,phone,state,description
                  not-a-date,https://x/1,Item 1,1,City,hoje,invalid,,,desc
                  """;
        var tmp = Path.GetTempFileName(); File.WriteAllText(tmp, csv);

        var r = ScrapedCsvReader.Load(tmp).ToList();
        r.Should().HaveCount(1);
        r[0].CapturedAtUtc.Should().BeNull();
        r[0].PostDate.Should().BeNull();
        r[0].Title.Should().Be("Item 1");
    }

    [Fact]
    public void Load_Ignores_Unknown_Extra_Columns()
    {
        var csv = """
                  captured_at_utc,url,title,ref_id,location,when,post_date,phone,state,description,extra
                  2025-08-21T00:00:00Z,https://x/1,Item 1,1,City,hoje,2025-08-21,,,desc,anything
                  """;
        var tmp = Path.GetTempFileName(); File.WriteAllText(tmp, csv);

        var r = ScrapedCsvReader.Load(tmp).ToList();
        r.Should().HaveCount(1);
        r[0].Title.Should().Be("Item 1");
        r[0].Url.Should().Be("https://x/1");
    }

}