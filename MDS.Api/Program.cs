using MDS.Application.Security;
using MDS.Application.Security.Interfaces;
using MDS.Application.Time;
using MDS.Infrastructure.Security;
using MDS.Infrastructure.Security.Interfaces;
using MDS.Infrastructure.Time;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddCorsPolicy("Default");
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<JwtOptions>>().Value);
builder.Services.AddSingleton<ITokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<ITokenService, TokenService>();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }