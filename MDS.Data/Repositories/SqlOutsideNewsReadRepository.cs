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
                // Altere 'PublishedAt' para 'CreatedAt' aqui
                "select top (@ps) Id, Title, Summary, Content, Source, Url, ImageUrl, PublishedAt, CreatedAt from dbo.News order by CreatedAt desc, Id desc",
                new { ps = pageSize },
                cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<OutsideNews?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!int.TryParse(id, out var newsId))
        {
            return null;
        }

        await using var cn = new SqlConnection(cfg.GetConnectionString("Default"));
        const string sql = "SELECT Id, Title, Summary, Content, Source, Url, ImageUrl, PublishedAt, CreatedAt FROM dbo.News WHERE Id = @Id";
        
        var result = await cn.QuerySingleOrDefaultAsync<OutsideNews>(
            new CommandDefinition(sql, new { Id = newsId }, cancellationToken: ct)
        );
        
        return result;
    }
}