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
public sealed class NewsController(IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>> handler) : ControllerBase
{
    private const int MaxPageSize = 50;

    [AllowAnonymous]
    [HttpGet("top")]
    [ServiceFilter(typeof(RequirePowFilter))]
    [ResponseCache(
        Duration = 60, 
        Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = new[] { "pageSize" } // 👈 garante cache distinto por querystring
    )]
    public async Task<ActionResult<IReadOnlyList<OutsideNews>>> GetTop([FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (pageSize <= 0) return Ok(new List<OutsideNews>());
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        try
        {
            var result = await handler.Handle(new GetTopNewsQuery(pageSize), ct);
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
}