using Dapper;
using MDS.Data.Context;
using MeDeixaSaber.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MDS.Data.Repositories;

public sealed class NewsRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<NewsRepository> _logger;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NewsRepository(IDbConnectionFactory factory, ILogger<NewsRepository>? logger = null)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? NullLogger<NewsRepository>.Instance;
    }

    public async Task InsertAsync(OutsideNews news)
    {
        try
        {
            var categoriesJson = news.Categories is { Count: > 0 }
                ? JsonSerializer.Serialize(news.Categories, JsonSerializerOptions)
                : null;

            var imageUrl = string.IsNullOrWhiteSpace(news.ImageUrl) ? null : news.ImageUrl;

            await using var conn = await _factory.GetOpenConnectionAsync();
            var p = new DynamicParameters();
            p.Add("@Url", news.Url);
            p.Add("@Title", news.Title);
            p.Add("@Summary", news.Summary);
            p.Add("@Content", news.Content);
            p.Add("@Source", news.Source);
            p.Add("@ImageUrl", imageUrl);
            p.Add("@PublishedAt", news.PublishedAt);
            p.Add("@CreatedAt", news.CreatedAt);
            p.Add("@CategoriesJson", categoriesJson);

            await conn.ExecuteAsync("dbo.sp_UpsertArticle", p, commandType: CommandType.StoredProcedure);
            _logger.LogInformation("Successfully upserted article: {Url}", news.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upsert article: {Url}", news.Url);
            throw;
        }
    }

    public async Task<bool> ExistsByUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            await using var conn = await _factory.GetOpenConnectionAsync();
            var exists = await conn.QueryFirstOrDefaultAsync<bool>(
                "SELECT CAST(COUNT(1) AS BIT) FROM dbo.News WHERE Url = @Url",
                new { Url = url });
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of article: {Url}", url);
            throw;
        }
    }
}