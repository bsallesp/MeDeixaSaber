using MeDeixaSaber.Core.Models;
using MDS.Runner.Scraper.Interfaces;

namespace MDS.Runner.Scraper.Services;

public sealed class DefaultScrapedCsvReader : IScrapedCsvReader
{
    public IEnumerable<Classified> Load(string path) => ScrapedCsvReader.Load(path);
}