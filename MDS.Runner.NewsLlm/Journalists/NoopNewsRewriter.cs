using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public sealed class NoopNewsRewriter : IOpenAiNewsRewriter
    {
        public Task<News> RewriteAsync(News source, EditorialBias bias, CancellationToken ct = default)
        {
            var clone = new News
            {
                Title = source.Title,
                Summary = source.Summary,
                Content = string.IsNullOrWhiteSpace(source.Content) ? "(sem conteúdo)" : source.Content,
                Source = source.Source,
                Url = source.Url,
                PublishedAt = source.PublishedAt == default ? DateTime.UtcNow : source.PublishedAt,
                CreatedAt = DateTime.UtcNow
            };
            return Task.FromResult(clone);
        }
    }
}