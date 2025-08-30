using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MDS.Api.Security.Hmac;

public sealed class HmacAuthFilter : IAsyncActionFilter
{
    readonly HmacSignatureValidator _validator;
    readonly IWebHostEnvironment _env;

    public HmacAuthFilter(HmacSignatureValidator validator, IWebHostEnvironment env)
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

        var ok = await _validator.ValidateAsync(context.HttpContext);
        if (!ok)
        {
            context.Result = new ObjectResult(new { error = "invalid_signature" }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        await next();
    }
}