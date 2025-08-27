using Dapper;
using MDS.Data.Context;
using MeDeixaSaber.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace MDS.Data.Repositories;

public sealed class NewsRepository(IDbConnectionFactory factory, ILogger<NewsRepository> logger)
{
    private readonly IDbConnectionFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    private readonly ILogger<NewsRepository> _logger = logger ?? NullLogger<NewsRepository>.Instance;

    public NewsRepository(IDbConnectionFactory factory)
        : this(factory, NullLogger<NewsRepository>.Instance) { }

    public async Task<IEnumerable<News>> GetByDayAsync(DateTime dayUtc)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<News>(
            "select * from dbo.News where cast(PublishedAt as date)=@d order by PublishedAt desc, Id desc",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<string>> GetTitlesByDayAsync(DateTime dayUtc)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<string>(
            "select Title from dbo.News where cast(PublishedAt as date)=@d",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<News>> GetLatestAsync(int take = 50)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<News>(
            "select top (@take) * from dbo.News order by PublishedAt desc, Id desc",
            new { take });
    }

    public async Task InsertAsync(News entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("News upsert start url={Url} title={Title}", entity.Url, entity.Title);

            const int timeoutSeconds = 15; // evita travar indefinidamente
            await using var conn = await _factory.GetOpenConnectionAsync();
            var rows = await conn.ExecuteAsync(
                "exec dbo.News_UpsertByUrl @Title,@Summary,@Content,@Source,@Url,@PublishedAt",
                new
                {
                    entity.Title,
                    entity.Summary,
                    entity.Content,
                    entity.Source,
                    entity.Url,
                    entity.PublishedAt
                },
                commandTimeout: timeoutSeconds);

            sw.Stop();
            _logger.LogInformation("News upsert ok url={Url} rows={Rows} ms={Elapsed}",
                entity.Url, rows, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "News upsert error url={Url} ms={Elapsed}", entity.Url, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task InsertManyAsync(IEnumerable<News> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var entity in items)
        {
            await InsertAsync(entity);
        }
    }
}
