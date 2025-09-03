using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public sealed class NoopNewsRewriter : IOpenAiNewsRewriter
    {
        public Task<OutsideNews> RewriteAsync(OutsideNews source, EditorialBias bias, CancellationToken ct = default)
        {
            var clone = new OutsideNews
            {
                Title = source.Title,
                Summary = source.Summary,
                Content = string.IsNullOrWhiteSpace(source.Content) ? "(sem conteúdo)" : source.Content,
                Source = source.Source,
                Url = source.Url,
                PublishedAt = source.PublishedAt == default ? DateTime.UtcNow : source.PublishedAt,
                ImageUrl = source.ImageUrl,
                CreatedAt = DateTime.UtcNow
            };
            return Task.FromResult(clone);
        }
    }
}