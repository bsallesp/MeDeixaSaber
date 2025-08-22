using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public interface INewsMapper
    {
        News? Map(NewsArticle article);
    }

    public sealed class NewsMapper : INewsMapper
    {
        public News? Map(NewsArticle article)
        {
            ArgumentNullException.ThrowIfNull(article);

            if (string.IsNullOrWhiteSpace(article.Title)) return null;
            if (string.IsNullOrWhiteSpace(article.Url)) return null;

            return new News
            {
                Title = article.Title,
                Summary = article.Description,
                Content = article.Content ?? string.Empty,
                Source = string.IsNullOrWhiteSpace(article.Source?.Name) ? "(unknown)" : article.Source!.Name,
                Url = article.Url,
                PublishedAt = article.PublishedAt == default ? DateTime.UtcNow : article.PublishedAt
            };
        }
    }
}