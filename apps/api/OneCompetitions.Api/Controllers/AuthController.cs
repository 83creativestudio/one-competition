using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneCompetitions.Api.Security;
using OneCompetitions.Application.Auth;
using OneCompetitions.Contracts.Auth;

namespace OneCompetitions.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        SetRefreshCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request.RefreshToken, HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.UserAgent.ToString(), cancellationToken);
        SetRefreshCookie(response.RefreshToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("one_competitions_refresh");
        return NoContent();
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<SessionResponse>>> Sessions(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetSessionsAsync(User.GetUserId(), cancellationToken));
    }

    [HttpDelete("sessions/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid id, CancellationToken cancellationToken)
    {
        await authService.RevokeSessionAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }

    private void SetRefreshCookie(string refreshToken)
    {
        Response.Cookies.Append("one_competitions_refresh", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        });
    }
}
