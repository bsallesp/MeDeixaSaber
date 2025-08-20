using MDS.Data.Data;
using MeDeixaSaber.Core.Models;

namespace MDS.Data.Repositories;

using Dapper;

public sealed class NewsRepository(IDbConnectionFactory factory)
{
    public async Task<IEnumerable<News>> GetByDayAsync(DateTime dayUtc)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<News>(
            "select * from dbo.News where cast(PublishedAt as date)=@d order by PublishedAt desc, Id desc",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<News>> GetLatestAsync(int take = 50)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<News>(
            "select top (@take) * from dbo.News order by PublishedAt desc, Id desc",
            new { take });
    }

    public async Task InsertAsync(News entity)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        await conn.ExecuteAsync(
            "exec dbo.News_UpsertByUrl @Title,@Summary,@Content,@Source,@Url,@PublishedAt",
            new
            {
                entity.Title,
                entity.Summary,
                entity.Content,
                entity.Source,
                entity.Url,
                entity.PublishedAt
            });
    }

    public async Task InsertManyAsync(IEnumerable<News> items)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        foreach (var entity in items)
        {
            await conn.ExecuteAsync(
                "exec dbo.News_UpsertByUrl @Title,@Summary,@Content,@Source,@Url,@PublishedAt",
                new
                {
                    entity.Title,
                    entity.Summary,
                    entity.Content,
                    entity.Source,
                    entity.Url,
                    entity.PublishedAt
                });
        }
    }
}