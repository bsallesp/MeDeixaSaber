using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using MDS.Data.Context;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;

namespace MDS.Data.Repositories;

public sealed class ClassifiedsRepository : IClassifiedsRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ITitleNormalizationService _normalizationService;

    public ClassifiedsRepository(IDbConnectionFactory factory, ITitleNormalizationService normalizationService)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
    }

    public async Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc)
    {
        await using var conn = await _factory.GetOpenConnectionAsync();
        var rows = await conn.QueryAsync<Classified>(
            "SELECT * FROM dbo.Classifieds WHERE CAST(CapturedAtUtc AS date) = @d",
            new { d = dayUtc.Date });

        foreach (var c in rows)
            c.Title = _normalizationService.Normalize(c.Title);

        return rows;
    }

    public async Task<IEnumerable<Classified>> GetLatestAsync(int take = 50)
    {
        if (take <= 0) throw new ArgumentOutOfRangeException(nameof(take));
        await using var conn = await _factory.GetOpenConnectionAsync();
        return await conn.QueryAsync<Classified>(
            "SELECT TOP (@take) * FROM dbo.Classifieds ORDER BY Id DESC",
            new { take });
    }

    public async Task InsertAsync(Classified entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        const string sql = @"
            INSERT INTO dbo.Classifieds
            (CapturedAtUtc, Url, Title, RefId, Location, ListingWhen, PostDate, Phone, State, Description, IsDuplicate)
            VALUES
            (@CapturedAtUtc, @Url, @Title, @RefId, @Location, @ListingWhen, @PostDate, @Phone, @State, @Description, @IsDuplicate);";

        entity.Title = _normalizationService.Normalize(entity.Title);

        await using var conn = await _factory.GetOpenConnectionAsync();
        try
        {
            await conn.ExecuteAsync(sql, entity);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2627)
        {
            throw new InvalidOperationException(
                $"Duplicate key violation for PostDate: {entity.PostDate}, Title: {entity.Title}", ex);
        }
    }
}

public interface IClassifiedsRepository
{
    Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc);
    Task<IEnumerable<Classified>> GetLatestAsync(int take = 50);
    Task InsertAsync(Classified entity);
}
