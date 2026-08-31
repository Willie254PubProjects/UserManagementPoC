using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using UserManagementPoC.Identity.Services;

using UserManagementPoC.Shared.Extensions;

using UserManagementPoC.Shared.Security.Contracts;

using UserManagementPoC.Shared.Security.Models;

using Microsoft.AspNetCore.RateLimiting;

namespace UserManagementPoC.Identity.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenGenerator _tokenGenerator;
    private readonly RefreshTokenService _refreshTokenService;
    private readonly IUserManagementApiClient _userManagementClient;
    private readonly IConfiguration _configuration;
    public AuthController(ITokenGenerator tokenGenerator,
        RefreshTokenService refreshTokenService,
        IUserManagementApiClient userManagementClient,
        IConfiguration configuration)
    {
        _tokenGenerator = tokenGenerator;
        _refreshTokenService = refreshTokenService;
        _userManagementClient = userManagementClient;
        _configuration = configuration;

    }
    [EnableRateLimiting("auth")]
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? clientId = null, [FromQuery] string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl)
            ? _configuration["OpenIdConnect:DefaultReturnUrl"]
            : returnUrl;

        if (string.IsNullOrEmpty(target) || SsoService.ValidateReturnUrl(target, clientId, _configuration) == null)
        {
            return this.ApiBadRequest("Return URL is not allowed");

        }
        var properties = new AuthenticationProperties { RedirectUri = target };
        if (!string.IsNullOrEmpty(clientId))
        {
            properties.Items["client_id"] = clientId;

        }
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);

    }
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var stored = await _refreshTokenService.ValidateAsync(request.RefreshToken);
        if (stored == null)
        {
            return this.ApiUnauthorized("Invalid or expired refresh token");

        }
        var parts = stored.Split('|', 2);
        var userId = parts[0];
        var securityVersion = parts.Length > 1 ? parts[1] : string.Empty;
        if (string.IsNullOrEmpty(securityVersion))
        {
            return this.ApiUnauthorized("Invalid or expired refresh token");

        }
        var session = await _userManagementClient.GetSessionAsync(securityVersion);
        if (session == null || !session.IsActive
            || !string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return this.ApiUnauthorized("Session invalid, expired, or logged out");

        }
        var user = await _userManagementClient.GetUserByIdAsync(userId);
        if (user == null)
        {
            return this.ApiUnauthorized("User not found");

        }
        var tokenResponse = await _tokenGenerator.GenerateTokenAsync(user, securityVersion);
        return this.ApiOk(tokenResponse);

    }
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var securityVersion = User.FindFirst("security_version")?.Value;
        if (string.IsNullOrEmpty(securityVersion))
        {
            return this.ApiUnauthorized("No active session");

        }
        await _userManagementClient.InvalidateSessionAsync(securityVersion);
        return this.ApiOk("Logged out successfully");

    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return this.ApiUnauthorized("User not authenticated");

        }
        var user = await _userManagementClient.GetUserByIdAsync(userId);
        if (user == null)
        {
            return this.ApiNotFound("User not found");

        }
        return this.ApiOk(user);
    }
}