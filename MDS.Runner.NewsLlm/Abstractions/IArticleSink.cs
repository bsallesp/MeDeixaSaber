using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Abstractions;

public interface IArticleSink
{
    Task InsertAsync(News item);
}