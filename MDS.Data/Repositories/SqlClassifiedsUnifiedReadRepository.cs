using System.Text.Json;
using Dapper;
using MDS.Application.Abstractions.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

using CoreDto = MDS.Application.Abstractions.Data.ClassifiedUnifiedDto;

namespace MDS.Data.Repositories;

public sealed class SqlClassifiedsUnifiedReadRepository(IConfiguration cfg) : IClassifiedsUnifiedReadRepository
{
    private static string[]? ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var arr = JsonSerializer.Deserialize<string[]>(json);
            return (arr is { Length: > 0 }) ? arr : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed record Row(string Title, string PostDate, string Description, string? Tags, string Url);

    public async Task<IReadOnlyList<CoreDto>> GetTopAsync(int take, int skip, CancellationToken ct = default)
    {
        await using var cn = new SqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<Row>(
            new CommandDefinition(
                """
                select Title, PostDate, Description, Tag as Tags, Url
                from dbo.ClassifiedsUnified
                order by convert(date, PostDate, 101) desc, Title asc
                offset @skip rows fetch next @take rows only
                """,
                new { take, skip },
                cancellationToken: ct));

        return rows
            .Select(r => new CoreDto(
                r.Title,
                r.PostDate,
                r.Description,
                ParseTags(r.Tags),
                r.Url))
            .ToList();
    }

    public async Task<IReadOnlyList<CoreDto>> GetByDayAsync(DateTime dayUtc, int take, int skip, CancellationToken ct = default)
    {
        await using var cn = new SqlConnection(cfg.GetConnectionString("Default"));
        var rows = await cn.QueryAsync<Row>(
            new CommandDefinition(
                """
                select Title, PostDate, Description, Tag as Tags, Url
                from dbo.ClassifiedsUnified
                where convert(date, PostDate, 101) = convert(date, @dayUtc)
                order by Title asc
                offset @skip rows fetch next @take rows only
                """,
                new { dayUtc, take, skip },
                cancellationToken: ct));

        return rows
            .Select(r => new CoreDto(
                r.Title,
                r.PostDate,
                r.Description,
                ParseTags(r.Tags),
                r.Url))
            .ToList();
    }
}
