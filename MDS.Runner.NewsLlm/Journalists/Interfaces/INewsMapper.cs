using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Journalists.Interfaces;

public interface INewsMapper
{
    OutsideNews? Map(NewsArticleDto articleDto);
}