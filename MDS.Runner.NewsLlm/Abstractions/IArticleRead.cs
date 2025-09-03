namespace MDS.Runner.NewsLlm.Abstractions;

public interface IArticleRead
{
    Task<bool> ExistsByUrlAsync(string url);
}