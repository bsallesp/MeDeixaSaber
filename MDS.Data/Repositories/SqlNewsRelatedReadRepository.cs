using System.Data;
using System.Data.SqlClient;
using MDS.Application.Abstractions.Data;
using MDS.Application.News.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MDS.Data.Repositories;

public sealed class SqlNewsRelatedReadRepository(IConfiguration cfg) : INewsRelatedReadRepository
{
    private readonly string _cs = cfg.GetConnectionString("Sql")
                                  ?? throw new InvalidOperationException("Missing ConnectionStrings:Sql");

    public async Task<IReadOnlyList<NewsRow>> GetLatestRelatedAsync(int daysBack, int topN, bool useContent, CancellationToken ct = default)
    {
        var list = new List<NewsRow>(topN + 1);

        await using var con = new SqlConnection(_cs);
        await con.OpenAsync(ct);

        await using var cmd = new SqlCommand("dbo.sp_LastNewsWithRelatedFTS_NewsFormat", con);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@DaysBack", daysBack);
        cmd.Parameters.AddWithValue("@TopN", topN);
        cmd.Parameters.AddWithValue("@UseContent", useContent);

        await using var rd = await cmd.ExecuteReaderAsync(ct);

        int iId = rd.GetOrdinal("Id");
        int iTitle = rd.GetOrdinal("Title");
        int iSummary = rd.GetOrdinal("Summary");
        int iContent = rd.GetOrdinal("Content");
        int iSource = rd.GetOrdinal("Source");
        int iUrl = rd.GetOrdinal("Url");
        int iPub = rd.GetOrdinal("PublishedAt");
        int iCreated = rd.GetOrdinal("CreatedAt");
        int iImg = rd.GetOrdinal("ImageUrl");

        while (await rd.ReadAsync(ct))
        {
            list.Add(new NewsRow(
                rd.GetInt32(iId),
                rd.GetString(iTitle),
                rd.IsDBNull(iSummary) ? null : rd.GetString(iSummary),
                rd.GetString(iContent),
                rd.GetString(iSource),
                rd.GetString(iUrl),
                rd.GetDateTime(iPub),
                rd.GetDateTime(iCreated),
                rd.IsDBNull(iImg) ? null : rd.GetString(iImg)
            ));
        }

        return list;
    }
}
