using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Interfaces;
public interface IScrapedCsvReader
{
    IEnumerable<Classified> Load(string path);
}