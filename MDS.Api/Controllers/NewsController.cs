using System.Security.Cryptography;
using System.Text;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MDS.Api.Filters;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NewsController(
    IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>> getTopHandler,
    IQueryHandler<GetNewsByIdQuery, OutsideNews?> getByIdHandler) : ControllerBase
{
    private const int MaxPageSize = 50;

    [AllowAnonymous]
    [HttpGet("top")]
    [ServiceFilter(typeof(RequirePowFilter))]
    [ResponseCache(
        Duration = 60,
        Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = ["pageSize"]
    )]
    public async Task<ActionResult<IReadOnlyList<OutsideNews>>> GetTop([FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (pageSize <= 0) return Ok(new List<OutsideNews>());
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        try
        {
            var result = await getTopHandler.Handle(new GetTopNewsQuery(pageSize), ct);
            var latest = result.Count == 0 ? DateTime.UnixEpoch : result.Max(x => x.CreatedAt);
            var raw = $"{latest.Ticks}:{result.Count}";
            var etag = $"W/\"news-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16]}\"";
            if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString().Contains(etag))
                return StatusCode(StatusCodes.Status304NotModified);
            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=120";
            return Ok(result);
        }
        catch
        {
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    [ResponseCache(
        Duration = 3600,
        Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = ["id"]
    )]
    public async Task<ActionResult<object>> GetById(string id, CancellationToken ct = default)
    {
        var result = await getByIdHandler.Handle(new GetNewsByIdQuery(id), ct);

        if (result is null)
        {
            return NotFound();
        }

        var raw = $"{result.Id}:{result.CreatedAt.Ticks}";
        var etag = $"W/\"news-item-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))[..16]}\"";
        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString().Contains(etag))
            return StatusCode(StatusCodes.Status304NotModified);
        Response.Headers.ETag = etag;

        var responseDto = new
        {
            result.Id,
            result.Title,
            result.Summary,
            result.Content,
            result.Source,
            result.Url,
            result.ImageUrl,
            publishedAt = result.PublishedAt,
            result.CreatedAt
        };

        return Ok(responseDto);
    }
}