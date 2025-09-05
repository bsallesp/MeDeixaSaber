using System.Security.Cryptography;
using System.Text;
using MDS.Api.Filters;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Models;
using MDS.Application.News.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/news/related")]
public sealed class NewsRelatedController(IQueryHandler<GetLatestRelatedNewsQuery, IReadOnlyList<NewsRow>> handler)
    : ControllerBase
{
    private const int MaxTop = 50;

    [AllowAnonymous]
    [HttpGet("latest")]
    [ServiceFilter(typeof(RequirePowFilter))]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any,
        VaryByQueryKeys = ["daysBack", "topN", "useContent"])]
    public async Task<ActionResult<IReadOnlyList<NewsRow>>> GetLatest(
        [FromQuery] int daysBack = 30,
        [FromQuery] int topN = 20,
        [FromQuery] int useContent = 0,
        CancellationToken ct = default)
    {
        if (daysBack <= 0) daysBack = 30;
        topN = Math.Clamp(topN, 1, MaxTop);
        var flag = useContent != 0;

        var result = await handler.Handle(new GetLatestRelatedNewsQuery(daysBack, topN, flag), ct);

        var basis = result.Count == 0 ? "empty" : $"{result[0].Id}:{result[^1].Id}:{result.Count}";
        var etag = $"W/\"rel-{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(basis)))[..16]}\"";

        if (Request.Headers.TryGetValue("If-None-Match", out var inm) && inm.ToString().Contains(etag))
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public, max-age=60, stale-while-revalidate=120";

        return Ok(result);
    }
}