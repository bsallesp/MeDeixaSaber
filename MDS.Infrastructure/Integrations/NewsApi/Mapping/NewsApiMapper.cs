using MeDeixaSaber.Core.Models;
using MDS.Infrastructure.Integrations.NewsApi.Dto;

namespace MDS.Infrastructure.Integrations.NewsApi.Mapping;

public sealed class NewsApiMapper
{
    public OutsideNews Map(NewsArticleDto dto)
    {
        return new OutsideNews
        {
            Title = dto.Title ?? "",
            Summary = dto.Description,
            Content = dto.Content ?? "",
            Source = dto.Source?.Name ?? "",
            Url = dto.Url ?? "",
            ImageUrl = dto.UrlToImage,
            PublishedAt = dto.PublishedAt,
            CreatedAt = DateTime.UtcNow
        };
    }
}
