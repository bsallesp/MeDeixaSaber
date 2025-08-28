namespace MDS.Runner.Scraper.Scrapers;

public sealed record ScrapeResult(
    string Site,
    string Date,
    int Pages,
    int TotalItems,
    string ItemsFile,
    string? LogFile
);