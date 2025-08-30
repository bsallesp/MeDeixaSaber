using MDS.Data.Repositories;
using MDS.Application.Abstractions.Data;
using MDS.Application.Abstractions.Messaging;
using MDS.Application.News.Queries;
using MeDeixaSaber.Core.Models;
using System.Threading.RateLimiting;
using MDS.Application.Security;
using MDS.Application.Security.Interfaces;
using MDS.Application.Time;
using MDS.Infrastructure.Security;
using MDS.Infrastructure.Security.Interfaces;
using MDS.Infrastructure.Time;
using Microsoft.Extensions.Options;
using MDS.Api.Security.Hmac;
using MDS.Api.Security.Pow;
using MDS.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendOnly", p =>
        p.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IQueryHandler<GetTopNewsQuery, IReadOnlyList<OutsideNews>>, GetTopNewsHandler>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenService, TokenService>();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<IOutsideNewsReadRepository, NullOutsideNewsReadRepository>();
}
else
{
    builder.Services.AddScoped<IOutsideNewsReadRepository, SqlOutsideNewsReadRepository>();
}

builder.Services.AddEndpointsApiExplorer();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddScoped<IClassifiedsUnifiedReadRepository, SqlClassifiedsUnifiedReadRepository>();
}

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

builder.Services.AddResponseCaching();

builder.Services.Configure<HmacSignatureOptions>(builder.Configuration.GetSection("Hmac"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IClientSecretProvider, InMemoryClientSecretProvider>();
builder.Services.AddSingleton<INonceStore, MemoryNonceStore>();
builder.Services.AddSingleton<HmacSignatureValidator>();
builder.Services.AddScoped<HmacAuthFilter>();
builder.Services.AddSingleton<IPowValidator, SimplePowValidator>();
builder.Services.AddScoped<MDS.Api.Filters.RequirePowFilter>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseCors("FrontendOnly");

if (!app.Environment.IsEnvironment("Testing"))
    app.UseRateLimiter();

app.UseResponseCaching();

app.UseMiddleware<BotDefenseMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}
