using Dapper;
using MDS.Data.Context;
using MeDeixaSaber.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using System.Data;
using System.Linq;

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

    public async Task<bool> ExistsByUrlAsync(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        await using var conn = await _factory.GetOpenConnectionAsync();
        var exists = await conn.ExecuteScalarAsync<int>(
            "select case when exists(select 1 from dbo.News where Url=@Url) then 1 else 0 end",
            new { Url = url });
        return exists == 1;
    }

    public async Task InsertAsync(OutsideNews entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var sw = Stopwatch.StartNew();
        
        const string newsUpsertSql = "dbo.News_UpsertByUrl";
        const string categoryUpsertSql = "dbo.sp_UpsertCategory";
        const string newsCategoryInsertSql = @"
            INSERT INTO NewsCategories (NewsId, CategoryId) 
            VALUES (@NewsId, @CategoryId)";
        
        await using var conn = await _factory.GetOpenConnectionAsync();
        IDbTransaction transaction = null!;

        try
        {
            transaction = conn.BeginTransaction();
            _logger.LogInformation("OutsideNews upsert start url={Url} title={Title}", entity.Url, entity.Title);

            // 1. Upsert (Insert/Update) da Notícia e obtenção do NewsId
            var newsId = await conn.ExecuteScalarAsync<int>(
                newsUpsertSql,
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
                commandType: CommandType.StoredProcedure,
                transaction: transaction,
                commandTimeout: 15);
            
            // 2. Inserção das Categorias
            if (entity.Categories != null && entity.Categories.Any())
            {
                var categoryIds = new List<int>();
                
                foreach (var category in entity.Categories)
                {
                    // Tenta obter/inserir a categoria e obter o CategoryId
                    // Assumimos que o sp_UpsertCategory retorna o CategoryId
                    var categoryId = await conn.ExecuteScalarAsync<int>(
                        categoryUpsertSql,
                        new { CategoryName = category.Name },
                        commandType: CommandType.StoredProcedure,
                        transaction: transaction);

                    if (categoryId > 0)
                    {
                        categoryIds.Add(categoryId);
                    }
                }

                // 3. Inserção na tabela de relacionamento NewsCategories
                var newsCategoryInserts = categoryIds
                    .Select(cId => new { NewsId = newsId, CategoryId = cId })
                    .ToList();

                var rowsInserted = await conn.ExecuteAsync(
                    newsCategoryInsertSql,
                    newsCategoryInserts,
                    transaction: transaction);
                
                _logger.LogInformation("OutsideNews categories inserted: {Count} rows.", rowsInserted);
            }

            transaction.Commit();

            sw.Stop();
            _logger.LogInformation("OutsideNews upsert ok url={Url} rows={Rows} ms={Elapsed}",
                entity.Url, newsId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            if (transaction != null)
            {
                transaction.Rollback();
            }
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