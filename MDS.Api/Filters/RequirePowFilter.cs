using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MDS.Api.Security.Pow;

namespace MDS.Api.Filters;

public sealed class RequirePowFilter : IAsyncActionFilter
{
    readonly IPowValidator _validator;
    readonly IWebHostEnvironment _env;

    public RequirePowFilter(IPowValidator validator, IWebHostEnvironment env)
    {
        _validator = validator;
        _env = env;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (_env.IsEnvironment("Testing"))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-PoW", out var pow))
        {
            context.Result = new ObjectResult(new { error = "missing_pow" }) { StatusCode = StatusCodes.Status428PreconditionRequired };
            return;
        }

        var ok = _validator.IsValid(pow.ToString(), 20, DateTimeOffset.UtcNow.ToUnixTimeSeconds(), 180);
        if (!ok)
        {
            context.Result = new ObjectResult(new { error = "invalid_pow" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}