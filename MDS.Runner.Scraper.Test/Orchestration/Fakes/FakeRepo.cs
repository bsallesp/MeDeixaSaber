using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MDS.Data.Repositories.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.Scraper.Test.Orchestration;

public sealed class FakeRepo(IEnumerable<Classified> existing) : IClassifiedsRepository
{
    public readonly List<Classified> Inserts = new();
    public Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc) => Task.FromResult(existing);
    public Task<IEnumerable<Classified>> GetLatestAsync(int take = 50) => Task.FromResult<IEnumerable<Classified>>(Array.Empty<Classified>());
    public Task InsertAsync(Classified entity) { Inserts.Add(entity); return Task.CompletedTask; }
}