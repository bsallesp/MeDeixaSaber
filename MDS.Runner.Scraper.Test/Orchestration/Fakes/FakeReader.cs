using System.Collections.Generic;
using System.Linq;
using MDS.Runner.Scraper.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Test.Orchestration.Fakes;

public sealed class FakeReader : IScrapedCsvReader
{
    private readonly Dictionary<string, IEnumerable<Classified>> _byPath;

    public FakeReader(Dictionary<string, IEnumerable<Classified>> byPath)
    {
        _byPath = byPath;
    }

    public IEnumerable<Classified> Load(string path) =>
        _byPath.TryGetValue(path, out var v) ? v : [];
}