using System.Threading.RateLimiting;
using MDS.Application.Security;
using MDS.Application.Security.Interfaces;
using MDS.Application.Time;
using MDS.Infrastructure.Security;
using MDS.Infrastructure.Security.Interfaces;
using MDS.Infrastructure.Time;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", p =>
        p.WithOrigins("https://seu-front.app")
         .AllowAnyHeader()
         .AllowAnyMethod());
});

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddEndpointsApiExplorer();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddJwtAuthentication(builder.Configuration);

    var rl = builder.Configuration.GetSection("RateLimit");
    var permitLimit = rl.GetValue("PermitLimit", 60);
    var windowSeconds = rl.GetValue("WindowSeconds", 60);

    builder.Services.AddRateLimiter(o =>
    {
        o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: key,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });
    });
}

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseCors("FrontendOnly");

if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
