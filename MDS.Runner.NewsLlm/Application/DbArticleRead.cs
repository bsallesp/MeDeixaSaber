using MDS.Data.Repositories;
using MDS.Runner.NewsLlm.Abstractions;

namespace MDS.Runner.NewsLlm.Application;

public sealed class DbArticleRead(NewsRepository repo) : IArticleRead
{
    private readonly NewsRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    public Task<bool> ExistsByUrlAsync(string url) => _repo.ExistsByUrlAsync(url);
}