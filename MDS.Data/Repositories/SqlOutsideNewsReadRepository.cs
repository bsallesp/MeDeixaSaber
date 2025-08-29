using Dapper;
using MDS.Application.Abstractions.Data;
using MeDeixaSaber.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MDS.Data.Repositories;

public sealed class SqlOutsideNewsReadRepository(IConfiguration cfg) : IOutsideNewsReadRepository
{
    public async Task<IReadOnlyList<OutsideNews>> GetTopAsync(int pageSize, CancellationToken ct = default)
    {
        await using var cn = new SqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<OutsideNews>(
            new CommandDefinition(
                "select top (@ps) Id, Title, Summary, Content, Source, Url, ImageUrl, PublishedAt, CreatedAt from dbo.News order by PublishedAt desc, Id desc",
                new { ps = pageSize },
                cancellationToken: ct));
        return rows.AsList();
    }
}

