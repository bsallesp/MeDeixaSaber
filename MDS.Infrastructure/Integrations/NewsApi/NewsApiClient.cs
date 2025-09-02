using System.Net.Http.Json;
using MeDeixaSaber.Core.Models;
using MDS.Application.Abstractions.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MDS.Infrastructure.Integrations.NewsApi.Mapping;
using Microsoft.Extensions.Options;

namespace MDS.Infrastructure.Integrations.NewsApi;

public sealed class NewsApiClient(HttpClient http, IOptions<NewsApiOptions> opt, NewsApiMapper mapper)
    : INewsProvider
{
    private readonly NewsApiOptions _opt = opt.Value;

    public async Task<IReadOnlyList<OutsideNews>> GetTopHeadlinesAsync(int pageSize, CancellationToken ct = default)
    {
        var url = $"top-headlines?country=us&pageSize={pageSize}&apiKey={_opt.ApiKey}";
        var resp = await http.GetFromJsonAsync<NewsApiResponseDto>(url, ct);
        if (resp == null || resp.Articles.Count == 0)
            return [];
        var list = new List<OutsideNews>(resp.Articles.Count);
        list.AddRange(resp.Articles.Select(mapper.Map));
        return list;
    }
}
