using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MDS.Api.Middleware;

public sealed class BotDefenseMiddleware
{
    readonly RequestDelegate _next;
    readonly IWebHostEnvironment _env;

    public BotDefenseMiddleware(RequestDelegate next, IWebHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!_env.IsEnvironment("Testing"))
        {
            var ua = context.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(ua)) { context.Response.StatusCode = StatusCodes.Status400BadRequest; await context.Response.WriteAsJsonAsync(new { error = "missing_user_agent" }); return; }

            if (context.Request.Path.StartsWithSegments("/api/classifieds"))
            {
                var take = GetInt(context.Request.Query, "take") ?? 20;
                var skip = GetInt(context.Request.Query, "skip") ?? 0;
                if (take > 50) { context.Response.StatusCode = StatusCodes.Status400BadRequest; await context.Response.WriteAsJsonAsync(new { error = "take_too_large" }); return; }
                if (skip < 0) { context.Response.StatusCode = StatusCodes.Status400BadRequest; await context.Response.WriteAsJsonAsync(new { error = "invalid_skip" }); return; }
            }

            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await Task.Delay(Random.Shared.Next(10, 60));
            }
        }

        await _next(context);
    }

    static int? GetInt(IQueryCollection q, string key)
    {
        if (!q.TryGetValue(key, out StringValues v)) return null;
        if (int.TryParse(v.ToString(), out var n)) return n;
        return null;
    }
}