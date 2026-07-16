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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        await authService.VerifyEmailAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("mfa/setup")]
    [AllowAnonymous]
    public async Task<ActionResult<MfaSetupResponse>> MfaSetup(MfaSetupRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.BeginMfaSetupAsync(request, cancellationToken));

    [HttpPost("mfa/enable")]
    [AllowAnonymous]
    public async Task<IActionResult> MfaEnable(MfaEnableRequest request, CancellationToken cancellationToken)
    {
        await authService.EnableMfaAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> MfaDisable(MfaDisableRequest request, CancellationToken cancellationToken)
    {
        await authService.DisableMfaAsync(User.GetUserId(), request, cancellationToken);
        return NoContent();
    }

    [HttpPost("invitations/accept")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        await authService.AcceptInvitationAsync(request, cancellationToken);
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
