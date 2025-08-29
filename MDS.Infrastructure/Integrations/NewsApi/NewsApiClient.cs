using System.Net.Http.Json;
using MeDeixaSaber.Core.Models;
using MDS.Application.Abstractions.Integrations;
using MDS.Infrastructure.Integrations.NewsApi.Dto;
using MDS.Infrastructure.Integrations.NewsApi.Mapping;
using Microsoft.Extensions.Options;

namespace MDS.Infrastructure.Integrations.NewsApi;

public sealed class NewsApiClient : INewsProvider
{
    private readonly HttpClient _http;
    private readonly NewsApiOptions _opt;
    private readonly NewsApiMapper _mapper;

    public NewsApiClient(HttpClient http, IOptions<NewsApiOptions> opt, NewsApiMapper mapper)
    {
        _http = http;
        _opt = opt.Value;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<OutsideNews>> GetTopHeadlinesAsync(int pageSize, CancellationToken ct = default)
    {
        var url = $"top-headlines?country=us&pageSize={pageSize}&apiKey={_opt.ApiKey}";
        var resp = await _http.GetFromJsonAsync<NewsApiResponseDto>(url, ct);
        if (resp == null || resp.Articles == null || resp.Articles.Count == 0)
            return Array.Empty<OutsideNews>();
        var list = new List<OutsideNews>(resp.Articles.Count);
        foreach (var a in resp.Articles)
            list.Add(_mapper.Map(a));
        return list;
    }
}
