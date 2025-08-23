using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using MDS.Application.Security;
using MDS.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MDS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ITokenService tokens, IOptions<JwtOptions> opt) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("token")]
    public ActionResult<TokenResponse> IssueToken([FromBody] LoginRequest req)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "Username and password are required." });

        // TODO: Validate credentials here

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, req.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = tokens.GenerateToken(claims);
        var expires = DateTime.UtcNow.AddMinutes(opt.Value.ExpirationMinutes);
        return Ok(new TokenResponse(token, expires));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<object> Me()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.Identity?.Name ?? "";
        return Ok(new { sub, claims = User.Claims.Select(c => new { c.Type, c.Value }) });
    }
}

public sealed record LoginRequest(
    [Required(ErrorMessage = "Username is required.")]
    string Username,
    [Required(ErrorMessage = "Password is required.")]
    string Password
);

public sealed record TokenResponse(string AccessToken, DateTime ExpiresAt);