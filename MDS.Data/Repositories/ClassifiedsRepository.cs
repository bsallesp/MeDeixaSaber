using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MDS.Data.Context;
using MDS.Data.Repositories.Interfaces;
using MeDeixaSaber.Core.Models;
using MeDeixaSaber.Core.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MDS.Data.Repositories;

public sealed class ClassifiedsRepository : IClassifiedsRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ITitleNormalizationService _normalizationService;
    private readonly ILogger<ClassifiedsRepository> _logger;

    public ClassifiedsRepository(IDbConnectionFactory factory, ITitleNormalizationService normalizationService)
        : this(factory, normalizationService, NullLogger<ClassifiedsRepository>.Instance) { }

    public ClassifiedsRepository(
        IDbConnectionFactory factory,
        ITitleNormalizationService normalizationService,
        ILogger<ClassifiedsRepository> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _normalizationService = normalizationService ?? throw new ArgumentNullException(nameof(normalizationService));
        _logger = logger ?? NullLogger<ClassifiedsRepository>.Instance;
    }

    public async Task<IEnumerable<Classified>> GetByDayAsync(DateTime dayUtc)
    {
        _logger.LogInformation("Fetching classifieds for {Day}", dayUtc.Date);
        await using var conn = await _factory.GetOpenConnectionAsync();
        var rows = await conn.QueryAsync<Classified>(
            "SELECT * FROM dbo.Classifieds WHERE CAST(CapturedAtUtc AS date) = @d",
            new { d = dayUtc.Date });

        foreach (var c in rows)
            c.Title = _normalizationService.Normalize(c.Title);

        _logger.LogDebug("Fetched {Count} classifieds for {Day}", rows.Count(), dayUtc.Date);
        return rows;
    }

    public async Task<IEnumerable<Classified>> GetLatestAsync(int take = 50)
    {
        if (take <= 0) throw new ArgumentOutOfRangeException(nameof(take));
        _logger.LogInformation("Fetching latest {Take} classifieds", take);
        await using var conn = await _factory.GetOpenConnectionAsync();
        var rows = await conn.QueryAsync<Classified>(
            "SELECT TOP (@take) * FROM dbo.Classifieds ORDER BY Id DESC",
            new { take });

        _logger.LogDebug("Fetched {Count} classifieds", rows.Count());
        return rows;
    }

    public async Task InsertAsync(Classified entity)
    {
        if (entity is null) throw new ArgumentNullException(nameof(entity));

        _logger.LogInformation("Inserting classified: {Title}", entity.Title);

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
            _logger.LogInformation("Inserted classified with RefId {RefId}", entity.RefId);
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            _logger.LogWarning(ex, "Duplicate classified detected: {Title} | RefId {RefId}", entity.Title, entity.RefId);
            throw new InvalidOperationException(
                $"Duplicate key violation for PostDate: {entity.PostDate}, Title: {entity.Title}", ex);
        }
    }
}
