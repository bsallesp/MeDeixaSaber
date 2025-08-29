using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NewsController(IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>> handler)
    : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("top")]
    public async Task<ActionResult<IReadOnlyList<OutsideNews>>> GetTop([FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        var query = new GetTopNewsQuery(pageSize);
        var result = await handler.Handle(query, ct);
        return Ok(result);
    }
}