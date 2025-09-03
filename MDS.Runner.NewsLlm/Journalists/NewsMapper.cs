using MDS.Infrastructure.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MDS.Runner.NewsLlm.Journalists.Interfaces;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists
{
    public sealed class NewsMapper : INewsMapper
    {
        public OutsideNews? Map(NewsArticleDto articleDto)
        {
            ArgumentNullException.ThrowIfNull(articleDto);

            if (string.IsNullOrWhiteSpace(articleDto.Title)) return null;
            if (string.IsNullOrWhiteSpace(articleDto.Url)) return null;

            return new OutsideNews
            {
                Title = articleDto.Title,
                Summary = articleDto.Description,
                Content = articleDto.Content ?? string.Empty,
                Source = string.IsNullOrWhiteSpace(articleDto.Source?.Name) ? "(unknown)" : articleDto.Source!.Name,
                Url = articleDto.Url,
                PublishedAt = articleDto.PublishedAt == default ? DateTime.UtcNow : articleDto.PublishedAt,
                ImageUrl = articleDto.UrlToImage
            };
        }
    }
}