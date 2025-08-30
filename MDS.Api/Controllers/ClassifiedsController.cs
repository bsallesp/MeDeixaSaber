using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MDS.Application.Abstractions.Data;
using MDS.Api.Security.Hmac;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[ServiceFilter(typeof(HmacAuthFilter))]
public sealed class ClassifiedsController(IClassifiedsUnifiedReadRepository repo) : ControllerBase
{
    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] int take = 20, [FromQuery] int skip = 0, CancellationToken ct = default)
    {
        if (take <= 0) return Ok(Enumerable.Empty<object>());
        skip = Math.Max(0, skip);
        var data = await repo.GetTopAsync(take, skip, ct);
        var shaped = data.Select(x => new
        {
            id = x.Url,
            title = x.Title,
            postDate = x.PostDate,
            description = x.Description,
            tags = x.Tags,
            url = x.Url
        });
        return Ok(shaped);
    }

    [HttpGet("by-day")]
    public async Task<IActionResult> GetByDay([FromQuery] string day, [FromQuery] int take = 50, [FromQuery] int skip = 0, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(day))
            return BadRequest(new { error = "day is required" });

        if (!DateTime.TryParse(day, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            return BadRequest(new { error = "invalid_day" });

        if (take <= 0) return Ok(Enumerable.Empty<object>());
        skip = Math.Max(0, skip);

        var data = await repo.GetByDayAsync(parsed, take, skip, ct);
        var shaped = data.Select(x => new
        {
            id = x.Url,
            title = x.Title,
            postDate = x.PostDate,
            description = x.Description,
            tags = x.Tags,
            url = x.Url
        });
        return Ok(shaped);
    }
}