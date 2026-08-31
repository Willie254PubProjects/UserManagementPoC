using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Shared.Authorization.Sso;

using UserManagementPoC.Shared.Security.Models;

namespace UserManagementAdmin.Controllers;

[Route("sso")]
public class SsoController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IdentitySsoClient _identitySsoClient;
    public SsoController(IConfiguration configuration, IdentitySsoClient identitySsoClient)
    {
        _configuration = configuration;
        _identitySsoClient = identitySsoClient;
    }
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? error = null, [FromQuery] string? reason = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/");

        }
        ViewData["Error"] = error;
        ViewData["Reason"] = reason;
        return View();
    }
    [HttpGet("begin")]
    public IActionResult Begin()
    {
        var identityAuthority = _configuration["IdentityAuthority"] ?? "https://localhost:7057";
        var clientId = _configuration["IdentityClient:ClientId"] ?? "usermanagement-admin";
        var callback = $"{Request.Scheme}://{Request.Host}/sso/callback";
        var target = $"{identityAuthority}/api/auth/login?clientId={Uri.EscapeDataString(clientId)}&returnUrl={Uri.EscapeDataString(callback)}";
        return Redirect(target);
    }
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, [FromQuery] string? error, [FromQuery] string? reason)
    {
        if (!string.IsNullOrEmpty(error))
        {
            var target = $"/sso/login?error={Uri.EscapeDataString(error)}";
            if (!string.IsNullOrEmpty(reason)) target += $"&reason={Uri.EscapeDataString(reason)}";
            return Redirect(target);

        }
        if (string.IsNullOrEmpty(code))
        {
            return Redirect("/sso/login?error=missing_code");

        }
        var tokenResponse = await _identitySsoClient.ExchangeCodeAsync(code);
        if (tokenResponse == null)
        {
            return Redirect("/sso/login?error=exchange_failed");

        }
        SetAuthCookies(tokenResponse);
        return Redirect("/");
    }
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await _identitySsoClient.LogoutAsync();
        Response.Cookies.Delete("access_token");
        Response.Cookies.Delete("refresh_token");
        Response.Cookies.Delete("expires_at");
        return Redirect("/sso/login");
    }
    private void SetAuthCookies(TokenResponse tokenResponse)
    {
        var secure = Request.IsHttps;
        var authCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        };
        Response.Cookies.Append("access_token", tokenResponse.AccessToken, authCookieOptions);
        Response.Cookies.Append("refresh_token", tokenResponse.RefreshToken, authCookieOptions);
        Response.Cookies.Append("expires_at", tokenResponse.ExpiresAt.ToString("O"), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }
}