using MDS.Data.Data;
using MeDeixaSaber.Core.Models;

namespace MDS.Data.Repositories;

using Dapper;

public sealed class ClassifiedsRepository(IDbConnectionFactory factory)
{
    public async Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<Classified>(
            "select * from dbo.Classifieds where cast(CapturedAtUtc as date)=@d",
            new { d = dayUtc.Date });
    }

    public async Task<IEnumerable<Classified>> GetLatestAsync(int take = 50)
    {
        await using var conn = await factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<Classified>(
            "select top (@take) * from dbo.Classifieds order by Id desc",
            new { take });
    }

    public async Task InsertAsync(Classified entity)
    {
        const string sql = @"
        insert into dbo.Classifieds
        (CapturedAtUtc, Url, Title, RefId, Location, ListingWhen, PostDate, Phone, State, Description, IsDuplicate)
        values
        (@CapturedAtUtc, @Url, @Title, @RefId, @Location, @ListingWhen, @PostDate, @Phone, @State, @Description, @IsDuplicate);";
        await using var conn = await factory.GetOpenConnectionAsync();
        await conn.ExecuteAsync(sql, entity);
    }
}