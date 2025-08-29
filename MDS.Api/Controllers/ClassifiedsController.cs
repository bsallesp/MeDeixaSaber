using Microsoft.AspNetCore.Mvc;
using MDS.Application.Abstractions.Data;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ClassifiedsController(IClassifiedsUnifiedReadRepository repo) : ControllerBase
{
    [HttpGet("top")]
    public async Task<IActionResult> GetTop([FromQuery] int take = 20, [FromQuery] int skip = 0, CancellationToken ct = default)
        => Ok(await repo.GetTopAsync(take, skip, ct));

    [HttpGet("by-day")]
    public async Task<IActionResult> GetByDay([FromQuery] DateTime day, [FromQuery] int take = 50, [FromQuery] int skip = 0, CancellationToken ct = default)
        => Ok(await repo.GetByDayAsync(day, take, skip, ct));
}
