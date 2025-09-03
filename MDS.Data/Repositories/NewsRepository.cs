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

    public async Task<IEnumerable<OutsideNews>> GetByDayAsync(DateTime dayUtc)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<OutsideNews>(
            "select * from dbo.OutsideNews where cast(PublishedAt as date)=@d order by PublishedAt desc, Id desc",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<string>> GetTitlesByDayAsync(DateTime dayUtc)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<string>(
            "select Title from dbo.OutsideNews where cast(PublishedAt as date)=@d",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<OutsideNews>> GetLatestAsync(int take = 50)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<OutsideNews>(
            "select top (@take) * from dbo.OutsideNews order by PublishedAt desc, Id desc",
            new { take });
    }

    public async Task InsertAsync(OutsideNews entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("OutsideNews upsert start url={Url} title={Title}", entity.Url, entity.Title);

            const int timeoutSeconds = 15; // evita travar indefinidamente
            await using var conn = await _factory.GetOpenConnectionAsync();
            var rows = await conn.ExecuteAsync(
                "exec dbo.News_UpsertByUrl @Title,@Summary,@Content,@Source,@Url,@PublishedAt,@ImageUrl",
                new
                {
                    entity.Title,
                    entity.Summary,
                    entity.Content,
                    entity.Source,
                    entity.Url,
                    entity.PublishedAt,
                    entity.ImageUrl
                },
                commandTimeout: timeoutSeconds);

            sw.Stop();
            _logger.LogInformation("OutsideNews upsert ok url={Url} rows={Rows} ms={Elapsed}",
                entity.Url, rows, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "OutsideNews upsert error url={Url} ms={Elapsed}", entity.Url, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task InsertManyAsync(IEnumerable<OutsideNews> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var entity in items)
        {
            await InsertAsync(entity);
        }
    }
}
