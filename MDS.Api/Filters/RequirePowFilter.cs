using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MDS.Api.Security.Pow;

namespace MDS.Api.Filters;

public sealed class RequirePowFilter(IPowValidator validator, IWebHostEnvironment env) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (env.IsEnvironment("Testing"))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-PoW", out var pow))
        {
            context.Result = new ObjectResult(new { error = "missing_pow" }) { StatusCode = StatusCodes.Status428PreconditionRequired };
            return;
        }

        var ok = validator.IsValid(pow.ToString(), 12, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 180);
        if (!ok)
        {
            context.Result = new ObjectResult(new { error = "invalid_pow" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}