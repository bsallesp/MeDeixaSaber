using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Abstractions;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application;

public sealed class ArticleSink(NewsRepository repo) : IArticleSink
{
    private readonly NewsRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public Task InsertAsync(OutsideNews item) => _repo.InsertAsync(item);
}