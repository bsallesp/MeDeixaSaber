using System;
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
            if (article is null) return null;
            if (string.IsNullOrWhiteSpace(article.Title)) return null;
            if (string.IsNullOrWhiteSpace(article.Url)) return null;
            var src = article.Source?.Name ?? "(unknown)";
            return new News
            {
                Title = article.Title,
                Summary = article.Description,
                Content = article.Content ?? string.Empty,
                Source = src,
                Url = article.Url,
                PublishedAt = article.PublishedAt == default ? DateTime.UtcNow : article.PublishedAt
            };
        }
    }
}